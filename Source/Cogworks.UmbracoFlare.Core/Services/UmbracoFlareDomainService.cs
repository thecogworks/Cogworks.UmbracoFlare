using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IUmbracoFlareDomainService
    {
        IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains);

        List<string> GetUrlsForNode(int contentId, bool includeDescendants = false);

        List<string> GetUrlsForNode(IContent content, bool includeDescendants = false);

        IEnumerable<Zone> GetAllowedCloudflareZones();

        IEnumerable<string> GetAllowedCloudflareDomains();
    }

    public class UmbracoFlareDomainService : IUmbracoFlareDomainService
    {
        private readonly ICloudflareService _cloudflareService;
        private readonly IDomainService _domainService;
        private readonly IContentService _umbracoContentService;

        public UmbracoFlareDomainService(ICloudflareService cloudflareService, IDomainService domainService, IContentService umbracoContentService)
        {
            _cloudflareService = cloudflareService;
            _domainService = domainService;
            _umbracoContentService = umbracoContentService;
        }

        public IEnumerable<string> FilterToAllowedDomains(IEnumerable<string> domains)
        {
            var filteredDomains = new List<string>();

            var allowedDomains = GetAllowedCloudflareDomains();

            foreach (var allowedDomain in allowedDomains)
            {
                foreach (var posDomain in domains)
                {
                    if (!posDomain.Contains(allowedDomain)) { continue; }

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
            var content = _umbracoContentService.GetById(contentId);
            return content == null ? new List<string>() : GetUrlsForNode(content, includeDescendants);
        }

        public List<string> GetUrlsForNode(IContent content, bool includeDescendants = false)
        {
            var urls = new List<string>();

            if (!content.HasValue()) { return urls; }

            var url = UmbracoContext.Current.RoutingContext.UrlProvider.GetUrl(content.Id, true);

            urls.AddRange(UrlHelper.MakeFullUrlWithDomain(url, RecursivelyGetParentsDomains(null, content)));
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

        private IEnumerable<string> RecursivelyGetParentsDomains(List<string> domains, IContent content)
        {
            if (!domains.HasAny())
            {
                domains = new List<string>();
            }

            if (!content.HasValue()) { return domains; }

            domains.AddRange(_domainService.GetAssignedDomains(content.Id, false).Select(x => x.DomainName));
            domains = RecursivelyGetParentsDomains(domains, content.Parent()) as List<string>;

            return domains;
        }

        private IEnumerable<string> GetDescendantsDomains(List<string> domains, IEnumerable<IContent> descendants)
        {
            if (!descendants.HasAny()) { return domains; }

            foreach (var descendant in descendants)
            {
                domains.AddRange(_domainService.GetAssignedDomains(descendant.Id, false).Select(x => x.DomainName));
            }

            return domains;
        }

        private KeyValuePair<IEnumerable<Zone>, IEnumerable<string>> GetAllowedZonesAndDomains()
        {
            var allowedZones = new List<Zone>();
            var allowedDomains = new List<string>();

            var allZones = _cloudflareService.GetCloudflareZones();

            var umbracoDomains = _domainService.GetAll(false).Select(x => new UriBuilder(x.DomainName).Uri.DnsSafeHost);

            foreach (var zone in allZones)
            {
                foreach (var domain in umbracoDomains)
                {
                    if (!domain.Contains(zone.Name)) { continue; }

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
    }
}