using Cogworks.UmbracoFlare.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Web;

namespace Cogworks.UmbracoFlare.Core.Helpers
{
    public class UmbracoFlareUrlHelper
    {
        public static string GetCurrentDomain()
        {
            var rootUrl = $"{HttpContext.Current.Request.Url.Scheme}{Uri.SchemeDelimiter}{HttpContext.Current.Request.Url.Host}";
            var isValidUri = Uri.TryCreate(rootUrl, UriKind.Absolute, out var uriWithDomain);
            var returnUrl = string.Empty;

            return !isValidUri ? returnUrl : uriWithDomain.ToString();
        }

        public static IEnumerable<string> GetFullUrlForPurgeStaticFiles(string url, string currentDomain, bool withScheme)
        {
            var urlsWithDomain = new List<string>();

            if (!url.HasValue()) { return urlsWithDomain; }

            urlsWithDomain.Add(MakeFullUrlWithDomain(url, currentDomain, withScheme));

            return urlsWithDomain;
        }

        public static IEnumerable<string> MakeFullUrlsWithDomain(IEnumerable<string> urls, string currentDomain, bool withScheme)
        {
            var urlsWithDomains = new List<string>();
            if (!urls.HasAny() || !currentDomain.HasValue()) { return urlsWithDomains; }

            foreach (var url in urls)
            {
                urlsWithDomains.Add(MakeFullUrlWithDomain(url, currentDomain, withScheme));
            }

            return urlsWithDomains;
        }

        public static string MakeFullUrlWithDomain(string url, string domain, bool withScheme)
        {
            if (!domain.HasValue()) { return url; }

            var returnUrl = string.Empty;
            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out var uriWithDomain);

            if (isValidUri)
            {
                if (uriWithDomain.Host.HasValue())
                {
                    returnUrl = CombinePaths(domain, uriWithDomain.PathAndQuery);
                }
            }
            else
            {
                returnUrl = CombinePaths(domain, url);
            }

            return withScheme ? AddSchemeToUrl(returnUrl) : returnUrl;
        }

        private static string AddSchemeToUrl(string url)
        {
            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out var uriWithDomain);

            if (isValidUri)
            {
                return uriWithDomain.Scheme.HasValue() ? url : $"{uriWithDomain.Scheme}://{url}";
            }

            return new UriBuilder(url).Scheme + "://" + url;
        }

        private static string CombinePaths(string path1, string path2)
        {
            if (path1.EndsWith("/") && path2.StartsWith("/"))
            {
                path1 = path1.TrimEnd('/');
            }
            else if (!path1.EndsWith("/") && !path2.StartsWith("/"))
            {
                path1 += "/";
            }

            return path1 + path2;
        }
    }
}