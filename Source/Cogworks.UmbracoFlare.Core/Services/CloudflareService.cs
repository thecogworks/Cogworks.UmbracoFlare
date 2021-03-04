using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface ICloudflareService
    {
        List<StatusWithMessage> PurgePages(IEnumerable<string> urls);

        StatusWithMessage PurgeEverything(string domain);

        string PrintResultsSummary(IEnumerable<StatusWithMessage> results);

        UserDetails GetCloudflareUserDetails(CloudflareConfigModel configurationFile);
    }

    public class CloudflareService : ICloudflareService
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;
        private readonly ICloudflareApiClient _cloudflareApiClient;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;

        public CloudflareService(IUmbracoLoggingService umbracoLoggingService, ICloudflareApiClient cloudflareApiClient, IUmbracoFlareDomainService umbracoFlareDomainService)
        {
            _umbracoLoggingService = umbracoLoggingService;
            _cloudflareApiClient = cloudflareApiClient;
            _umbracoFlareDomainService = umbracoFlareDomainService;
        }

        public List<StatusWithMessage> PurgePages(IEnumerable<string> urls)
        {
            var results = new List<StatusWithMessage>();

            urls = _umbracoFlareDomainService.FilterToAllowedDomains(urls);
            var groupings = urls.GroupBy(url => UrlHelper.GetDomainFromUrl(url, true));

            foreach (var domainUrlGroup in groupings)
            {
                var domainUrl = new UriBuilder(domainUrlGroup.Key).Uri;
                var websiteZone = GetZoneFilteredByDomain(domainUrl.DnsSafeHost);

                if (websiteZone == null)
                {
                    results.Add(new StatusWithMessage(false, $"Could not retrieve the zone from cloudflare with the domain(url) of {domainUrl}"));
                    continue;
                }

                var apiResult = _cloudflareApiClient.PurgeCache(websiteZone.Id, domainUrlGroup, false);

                if (!apiResult)
                {
                    results.Add(new StatusWithMessage(false, ApplicationConstants.CloudflareMessages.CloudflareApiError));
                }
                else
                {
                    results.AddRange(domainUrlGroup.Select(url => new StatusWithMessage(true, $"The url => {url} was purged successfully")));
                }
            }

            return results;
        }

        public StatusWithMessage PurgeEverything(string domain)
        {
            var websiteZone = GetZoneFilteredByDomain(domain);

            if (websiteZone == null)
            {
                return new StatusWithMessage(
                    false,
                    $"We could not purge the cache because the domain {domain} is not valid with the provided credentials. Please ensure this domain is registered under these credentials on your cloudflare dashboard.");
            }

            var purgeCacheStatus = _cloudflareApiClient.PurgeCache(websiteZone.Id, Enumerable.Empty<string>(), true);

            return purgeCacheStatus
                ? new StatusWithMessage(true, string.Empty)
                : new StatusWithMessage(false, ApplicationConstants.CloudflareMessages.CloudflareApiError);
        }

        public string PrintResultsSummary(IEnumerable<StatusWithMessage> results)
        {
            var statusMessages = new List<string>
            {
                $"There were {results.Count(x => x.Success)} successes."
            };

            statusMessages.AddRange(results.Where(x => !x.Success).Select(failedStatus => "Failed for reason: " + failedStatus.Message + ".  "));
            var resultsSummary = string.Join(" ", statusMessages);

            return resultsSummary;
        }

        public UserDetails GetCloudflareUserDetails(CloudflareConfigModel configurationFile)
        {
            return _cloudflareApiClient.GetUserDetails(configurationFile);
        }

        private Zone GetZoneFilteredByDomain(string domainUrl)
        {
            var allowedZones = _umbracoFlareDomainService.GetAllowedCloudflareZones();
            var filteredZonesByDomainUrl = allowedZones.Where(x => domainUrl.Contains(x.Name)).ToList();

            if (filteredZonesByDomainUrl.HasAny())
            {
                return filteredZonesByDomainUrl.First(x => x.Name == domainUrl);
            }

            var noZoneException = new Exception($"Could not retrieve the zone from cloudflare with the domain(url) of {filteredZonesByDomainUrl}");
            _umbracoLoggingService.LogError<ICloudflareService>(noZoneException.Message, noZoneException);

            return null;
        }
    }
}