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
using Cogworks.UmbracoFlare.Core.Models.Cloudflare;
using Umbraco.Core.Services;
using Umbraco.Web;

// ReSharper disable PossibleMultipleEnumeration
namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoFlareDomainService
    {
        IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains);

        IEnumerable<string> GetUrlsForNode(int contentId, bool includeDescendants = false);

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

        //TODO: IMPORTANT CACHE THIS!!
        public IEnumerable<string> GetUrlsForNode(int contentId, bool includeDescendants = false)
        {
            var content = _umbracoHelperWrapper.TypedContent(contentId);
            var urls = new List<string>();
            if (!content.HasValue()) { return urls; }
            
            if (includeDescendants)
            {
                foreach (var descendantContent in content.DescendantsOrSelf())
                {
                    urls.Add(UmbracoContext.Current.RoutingContext.UrlProvider.GetUrl(descendantContent.Id, true));
                    urls.AddRange(UmbracoContext.Current.RoutingContext.UrlProvider.GetOtherUrls(descendantContent.Id));
                }
            }

            return urls;
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

                var wildCardUrlTrimmed = wildCardUrl.TrimEnd('/').TrimEnd('*');

                resolvedUrls.AddRange(allContentUrls.Where(x => x.StartsWith(wildCardUrlTrimmed)));
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

        //TODO: IMPORTANT CACHE THIS !!!!!
        private IEnumerable<string> GetAllContentUrls()
        {
            var urls = new List<string>();
            var roots = _umbracoHelperWrapper.TypedContentAtRoot();

            foreach (var root in roots)
            {
                var allContent = root.DescendantsOrSelf();
                urls.AddRange(allContent.Select(content => content.Url));
            }

            return urls;
        }
    }
}