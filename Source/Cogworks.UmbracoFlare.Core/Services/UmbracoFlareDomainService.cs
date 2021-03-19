using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models.Cloudflare;
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
        IEnumerable<string> GetUrlsForNode(int contentId, string currentDomain, bool includeDescendants = false);

        IEnumerable<string> GetAllowedCloudflareDomains();

        IEnumerable<string> GetAllUrlsForWildCardUrls(IEnumerable<string> wildCardUrls);

        Zone GetZoneFilteredByDomain(string domainUrl);
    }

    public class UmbracoFlareDomainService : IUmbracoFlareDomainService
    {
        private readonly UmbracoHelper _umbracoHelper;
        private readonly ICloudflareApiClient _cloudflareApiClient;
        private readonly IDomainService _domainService;
        private readonly IUmbracoLoggingService _umbracoLoggingService;

        public UmbracoFlareDomainService(UmbracoHelper umbracoHelper, ICloudflareApiClient cloudflareApiClient, IDomainService domainService, IUmbracoLoggingService umbracoLoggingService)
        {
            _umbracoHelper = umbracoHelper;
            _cloudflareApiClient = cloudflareApiClient;
            _domainService = domainService;
            _umbracoLoggingService = umbracoLoggingService;
        }

        public IEnumerable<string> GetUrlsForNode(int contentId, string currentDomain, bool includeDescendants = false)
        {
            var content = _umbracoHelper.Content(contentId);
            var urls = new List<string>();

            if (!content.HasValue()) { return urls; }

            if (includeDescendants)
            {
                urls.AddRange(content.DescendantsOrSelf().Select(
                    descendantContent => UmbracoFlareUrlHelper.MakeFullUrlWithDomain(descendantContent.Url(), currentDomain, true))
                );
            }
            else
            {
                urls.Add(UmbracoFlareUrlHelper.MakeFullUrlWithDomain(content.Url(), currentDomain, true));
            }

            return urls;
        }

        public IEnumerable<string> GetAllowedCloudflareDomains()
        {
            var allowedZonesAndDomains = GetAllowedZonesAndDomains();
            var allowedDomains = allowedZonesAndDomains.Value;

            return allowedDomains;
        }

        public Zone GetZoneFilteredByDomain(string domainUrl)
        {
            var allowedZones = GetAllowedCloudflareZones();
            var filteredZonesByDomainUrl = allowedZones.Where(x => domainUrl.Contains(x.Name)).ToList();

            if (filteredZonesByDomainUrl.HasAny())
            {
                return filteredZonesByDomainUrl.FirstOrDefault();
            }

            var noZoneException = new Exception($"Could not retrieve the zone from cloudflare with the domain of {domainUrl}");
            _umbracoLoggingService.LogError<IUmbracoFlareDomainService>(noZoneException.Message, noZoneException);

            return null;
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

        private IEnumerable<Zone> GetAllowedCloudflareZones()
        {
            var allowedZonesAndDomains = GetAllowedZonesAndDomains();
            var allowedZones = allowedZonesAndDomains.Key;

            return allowedZones;
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

        private IEnumerable<string> GetAllContentUrls()
        {
            var urls = new List<string>();
            var roots = _umbracoHelper.ContentAtRoot();

            foreach (var root in roots)
            {
                var allContent = root.DescendantsOrSelf();
                urls.AddRange(allContent.Select(content => content.Url()));
            }

            return urls;
        }
    }
}