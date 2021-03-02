using Cogworks.UmbracoFlare.Core.Extensions;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Cogworks.UmbracoFlare.Core.Helpers
{
    //MOVE THIS TO A URLSERVICE THIS DOES TOO MUCH TO BE A HELPER
    public class UrlHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Takes the given url and returns the domain with the scheme (no path and query)
        /// ex. http://www.example.com/blah/blah?blah=blah will return http://www.example.com(/)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="withTrailingSlash"></param>
        /// <returns></returns>
        public static string GetDomainFromUrl(string url, bool withScheme = false, bool withTrailingSlash = false)
        {
            Uri uri;

            try
            {
                uri = new UriBuilder(url).Uri;
            }
            catch (Exception)
            {
                return MakeFullUrlWithDomain(url, null, true, withScheme);
            }

            if (withScheme)
            {
                return AddSchemeToUrl(uri.ToString()) + (withTrailingSlash ? "/" : "");
            }

            return uri.DnsSafeHost + (withTrailingSlash ? "/" : "");
        }

        public static string GetCurrentDomainWithScheme(bool withTrailingSlash = false)
        {
            var currentDomain = MakeFullUrlWithDomain("", null, true, true);

            if (!withTrailingSlash) { return currentDomain; }

            if (currentDomain[currentDomain.Length - 1] != '/')
            {
                currentDomain += "/";
            }

            return currentDomain;
        }

        public static IEnumerable<string> MakeFullUrlWithDomain(IEnumerable<string> urls, string host, bool withScheme = false)
        {
            return urls?.Select(url => MakeFullUrlWithDomain(url, host, !host.HasValue(), withScheme)).ToList();
        }

        public static IEnumerable<string> MakeFullUrlWithDomain(string url, IEnumerable<string> hosts, bool withScheme = false)
        {
            var urlsWithDomain = new List<string>();

            if (!url.HasValue())
            {
                return urlsWithDomain;
            }

            if (!hosts.HasAny())
            {
                //there aren't any hosts passed in so use the current domain.
                var currentHost = MakeFullUrlWithDomain(url, null, true, false);
                urlsWithDomain.Add(currentHost);

                return urlsWithDomain;
            }

            urlsWithDomain.AddRange(hosts.Select(host => MakeFullUrlWithDomain(url, host, withScheme: withScheme)));

            return urlsWithDomain;
        }

        public static IEnumerable<string> MakeFullUrlWithDomain(IEnumerable<string> urls, IEnumerable<string> hosts, bool withScheme = false)
        {
            var urlsWithDomains = new List<string>();

            if (!urls.HasAny() || !hosts.Any())
            {
                return urlsWithDomains;
            }

            foreach (var url in urls)
            {
                foreach (var host in hosts)
                {
                    urlsWithDomains.Add(MakeFullUrlWithDomain(url, host, withScheme: withScheme));
                }
            }

            return urlsWithDomains;
        }

        // Uses the HttpContext.Current to add on the scheme & host to the given url.
        public static string MakeFullUrlWithDomain(string url, string host, bool useCurrentDomain = false, bool withScheme = false)
        {
            if (!host.HasValue() && !useCurrentDomain)
            {
                return url;
            }

            if (useCurrentDomain && host.HasValue())
            {
                throw new Exception("If you are using the current domain, you CANNOT pass in a host as well.");
            }

            var returnUrl = "";
            Uri uriWithDomain;

            try
            {
                uriWithDomain = new Uri(url);

                if (uriWithDomain.Host.HasValue())
                {
                    //The url already has a host, but we want it to have the host we passed in.
                    returnUrl = CombinePaths(host, uriWithDomain.PathAndQuery);
                }
            }
            catch
            {
                // ignored
            }

            //if we made it here we know that the host was not added to the url.
            try
            {
                if (useCurrentDomain)
                {
                    if (HttpContext.Current == null)
                    {
                        Log.Error("HttpContext.Current or HttpContext.Current.Request is null.");
                    }

                    var root = new Uri($"{HttpContext.Current.Request.Url.Scheme}{Uri.SchemeDelimiter}{HttpContext.Current.Request.Url.Host}");

                    uriWithDomain = new Uri(root, url);

                    returnUrl = uriWithDomain.ToString();
                }
                else
                {
                    returnUrl = CombinePaths(host, url);
                }
            }
            catch
            {
                Log.Error($"Could not create root uri using http context request url {HttpContext.Current?.Request.Url}");
                return string.Empty;
            }

            return withScheme ? AddSchemeToUrl(returnUrl) : returnUrl;
        }

        public static string AddSchemeToUrl(string url)
        {
            try
            {
                var urlHasScheme = new Uri(url).Scheme;

                if (urlHasScheme.HasValue())
                {
                    //It already has a scheme
                    return url;
                }

                return new UriBuilder(url).Scheme + "://" + url;
            }
            catch (Exception)
            {
                return new UriBuilder(url).Scheme + "://" + url;
            }
        }

        public static string CombinePaths(string path1, string path2)
        {
            if (path1.EndsWith("/") && path2.StartsWith("/"))
            {
                //strip the first / so they aren't doubled up when we combine them.
                path1 = path1.TrimEnd('/');
            }
            else if (!path1.EndsWith("/") && !path2.StartsWith("/"))
            {
                //neither of them had a / so we have to add one.
                path1 += "/";
            }

            return path1 + path2;
        }
    }
}