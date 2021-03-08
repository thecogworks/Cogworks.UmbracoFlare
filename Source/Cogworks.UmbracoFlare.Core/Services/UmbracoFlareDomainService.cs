using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Factories;
using Cogworks.UmbracoFlare.Core.Models.Cloudflare;
using Cogworks.UmbracoFlare.Core.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Services;
using Umbraco.Web;

// ReSharper disable PossibleMultipleEnumeration
namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoFlareDomainService
    {
        IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains);

        IEnumerable<string> GetUrlsForNode(int contentId, bool includeDescendants = false);

        IEnumerable<string> GetUmbracoDomains();

        IEnumerable<Zone> GetAllowedCloudflareZones();

        IEnumerable<string> GetAllowedCloudflareDomains();

        IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls);
    }

    public class UmbracoFlareDomainService : IUmbracoFlareDomainService
    {
        private readonly ICloudflareApiClient _cloudflareApiClient;
        private readonly IDomainService _domainService;
        private readonly IUmbracoHelperWrapper _umbracoHelperWrapper;

        public UmbracoFlareDomainService()
        {
            _umbracoHelperWrapper = ServiceFactory.GetUmbracoHelperWrapper();
            _cloudflareApiClient = ServiceFactory.GetCloudflareApiClient();
            _domainService = ServiceFactory.GetDomainService();
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
            else
            {
                urls.Add(UmbracoContext.Current.RoutingContext.UrlProvider.GetUrl(content.Id, true));
                urls.AddRange(UmbracoContext.Current.RoutingContext.UrlProvider.GetOtherUrls(content.Id));
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

        public IEnumerable<string> GetUmbracoDomains()
        {
            var umbracoDomains = _domainService.GetAll(false);
            var umbracoDomainNames = umbracoDomains.Select(umbracoDomain => umbracoDomain.DomainName).ToList();
            return umbracoDomainNames;
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

                var wildCardUrlTrimmed = wildCardUrl.TrimEnd('*');
                wildCardUrlTrimmed = wildCardUrlTrimmed.TrimEnd('/');

                resolvedUrls.AddRange(allContentUrls.Where(x => x.StartsWith(wildCardUrlTrimmed)));
            }

            return resolvedUrls;
        }

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