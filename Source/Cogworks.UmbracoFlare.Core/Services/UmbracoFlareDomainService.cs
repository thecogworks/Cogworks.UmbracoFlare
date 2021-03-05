using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoFlareDomainService
    {
        IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains);

        List<string> GetUrlsForNode(int contentId, bool includeDescendants = false);

        List<string> GetUrlsForNode(IPublishedContent content, bool includeDescendants = false);

        IEnumerable<Zone> GetAllowedCloudflareZones();

        IEnumerable<string> GetAllowedCloudflareDomains();

        IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls);

        void UpdateContentIdToUrlCache(int id, IEnumerable<string> urls);

        void DeletedContentIdToUrlCache();
    }

    public class UmbracoFlareDomainService : IUmbracoFlareDomainService
    {
        private readonly ICloudflareApiClient _cloudflareApiClient;
        private readonly IDomainService _domainService;
        private readonly IUmbracoHelperWrapper _umbracoHelperWrapper;
        private Dictionary<int, IEnumerable<string>> _contentIdToUrlCache;

        public UmbracoFlareDomainService(IUmbracoHelperWrapper umbracoHelperWrapper, ICloudflareApiClient cloudflareApiClient, IDomainService domainService)
        {
            _umbracoHelperWrapper = umbracoHelperWrapper;
            _cloudflareApiClient = cloudflareApiClient;
            _domainService = domainService;
        }

        public IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains)
        {
            var filteredDomains = new List<string>();
            var allowedDomains = GetAllowedCloudflareDomains();

            foreach (var allowedDomain in allowedDomains)
            {
                foreach (var posDomain in domains.Where(posDomain => posDomain.Contains(allowedDomain)))
                {
                    if (!filteredDomains.Contains(posDomain))
                    {
                        filteredDomains.Add(posDomain);
                    }
                }
            }

            return filteredDomains;
        }

        public List<string> GetUrlsForNode(int contentId, bool includeDescendants = false)
        {
            var content = _umbracoHelperWrapper.TypedContent(contentId);
            return content == null ? new List<string>() : GetUrlsForNode(content, includeDescendants);
        }

        public List<string> GetUrlsForNode(IPublishedContent content, bool includeDescendants = false)
        {
            var urls = new List<string>();

            if (!content.HasValue()) { return urls; }

            var url = UmbracoContext.Current.RoutingContext.UrlProvider.GetUrl(content.Id, true);

            urls.AddRange(UrlHelper.GetFullUrlForPurgeFromContentNode(url, RecursivelyGetParentsDomains(null, content)));
            urls.AddRange(UmbracoContext.Current.RoutingContext.UrlProvider.GetOtherUrls(content.Id));

            if (includeDescendants)
            {
                //TODO: Descendants is very inefficient, find a way around
                foreach (var descendant in content.Descendants())
                {
                    urls.Add(UmbracoContext.Current.RoutingContext.UrlProvider.GetUrl(descendant.Id, true));
                    urls.AddRange(UmbracoContext.Current.RoutingContext.UrlProvider.GetOtherUrls(descendant.Id));
                }
            }

            return urls;
        }

        private IEnumerable<string> RecursivelyGetParentsDomains(List<string> domains, IPublishedContent content)
        {
            if (!domains.HasAny())
            {
                domains = new List<string>();
            }

            if (!content.HasValue()) { return domains; }

            domains.AddRange(_domainService.GetAssignedDomains(content.Id, false).Select(x => x.DomainName));
            domains = RecursivelyGetParentsDomains(domains, content.Parent) as List<string>;

            return domains;
        }

        public IEnumerable<Zone> GetAllowedCloudflareZones()
        {
            var allowedZonesAndDomains = GetAllowedZonesAndDomains();
            var allowedZones = allowedZonesAndDomains.Key;

            return allowedZones;
        }

        public IEnumerable<string> GetAllowedCloudflareDomains()
        {
            var allowedZonesAndDomains = GetAllowedZonesAndDomains();
            var allowedDomains = allowedZonesAndDomains.Value;

            return allowedDomains;
        }

        private KeyValuePair<IEnumerable<Zone>, IEnumerable<string>> GetAllowedZonesAndDomains()
        {
            var allowedZones = new List<Zone>();
            var allowedDomains = new List<string>();
            var allZones = _cloudflareApiClient.GetZones();
            var umbracoDomains = _domainService.GetAll(false).Select(x => new UriBuilder(x.DomainName).Uri.DnsSafeHost).ToList();

            foreach (var zone in allZones)
            {
                foreach (var domain in umbracoDomains.Where(domain => domain.Contains(zone.Name)))
                {
                    if (!allowedZones.Contains(zone))
                    {
                        allowedZones.Add(zone);
                    }

                    if (!allowedDomains.Contains(domain))
                    {
                        allowedDomains.Add(domain);
                    }
                }
            }

            return new KeyValuePair<IEnumerable<Zone>, IEnumerable<string>>(allowedZones, allowedDomains);
        }

        public IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls)
        {
            var resolvedUrls = new List<string>();
            if (!wildCardUrls.HasAny()) { return resolvedUrls; }

            var allContentUrls = GetAllContentUrls();

            foreach (var wildCardUrl in wildCardUrls)
            {
                if (!wildCardUrl.Contains('*')) { continue; }

                //Make one for modifying
                var mutableWildCardUrl = wildCardUrl;

                mutableWildCardUrl = mutableWildCardUrl.TrimEnd('/');
                mutableWildCardUrl = mutableWildCardUrl.TrimEnd('*');

                //We can get wild cards by seeing if any of the urls start with the mutable wild card url
                resolvedUrls.AddRange(allContentUrls.Where(x => x.StartsWith(mutableWildCardUrl)));
            }

            return resolvedUrls;
        }

        public void UpdateContentIdToUrlCache(int id, IEnumerable<string> urls)
        {
            if (!_contentIdToUrlCache.HasAny()) { return; }

            if (_contentIdToUrlCache.ContainsKey(id))
            {
                _contentIdToUrlCache[id] = urls;
            }
            else
            {
                _contentIdToUrlCache.Add(id, urls);
            }
        }

        public void DeletedContentIdToUrlCache()
        {
            HttpRuntime.Cache.Remove(ApplicationConstants.CacheKeys.UmbracoUrlWildCardServiceCacheKey);
            _contentIdToUrlCache = null;
        }

        //CACHE THIS METHOD BETTER
        private IEnumerable<string> GetAllContentUrls()
        {
            //if (_contentIdToUrlCache != null && _contentIdToUrlCache.Any())
            //{
            //    //just return the cache
            //    return this._contentIdToUrlCache.SelectMany(x => x.Value);
            //}

            //var cache = new Dictionary<int, IEnumerable<string>>();
            //var urls = new List<string>();

            ////Id like to use UmbracoContext.Current.ContentCache.GetByRoute() somehow but you cant always guarantee that urls
            ////will be in  hierarchical order because of rewriting, etc.
            //var roots = _umbracoHelperWrapper.TypedContentAtRoot();

            //foreach (var content in roots)
            //{
            //    var contentUrls = _umbracoFlareDomainService.GetUrlsForNode(content.Id);

            //    cache.Add(content.Id, contentUrls);
            //    urls.AddRange(contentUrls);

            //    foreach (var childContent in content.Descendants())
            //    {
            //        var childContentUrls = _umbracoFlareDomainService.GetUrlsForNode(childContent.Id);

            //        cache.Add(childContent.Id, childContentUrls);
            //        urls.AddRange(childContentUrls);
            //    }
            //}

            //_contentIdToUrlCache = cache;
            ////Add to the cache
            //HttpRuntime.Cache.Insert(ApplicationConstants.CacheKeys.UmbracoUrlWildCardServiceCacheKey,
            //    cache, null, DateTime.Now.AddDays(1),
            //    System.Web.Caching.Cache.NoSlidingExpiration,
            //    System.Web.Caching.CacheItemPriority.Normal,
            //    null);

            return new List<string>();
        }
    }
}