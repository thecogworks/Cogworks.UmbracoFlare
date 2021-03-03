using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface ICloudflareService
    {
        List<StatusWithMessage> PurgePages(IEnumerable<string> urls, bool purgeCacheOn = false);

        StatusWithMessage PurgeEverything(string domain);

        string PrintResultsSummary(IEnumerable<StatusWithMessage> results);

        UserDetails GetCloudflareUserDetails(CloudflareConfigModel configurationFile);
    }

    public class CloudflareService : ICloudflareService
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;
        private readonly ICloudflareApiClient _cloudflareApiClient;
        
        public CloudflareService(IUmbracoLoggingService umbracoLoggingService, ICloudflareApiClient cloudflareApiClient)
        {
            _umbracoLoggingService = umbracoLoggingService;
            _cloudflareApiClient = cloudflareApiClient;
        }

        public List<StatusWithMessage> PurgePages(IEnumerable<string> urls, bool purgeCacheOn = false)
        {
            var results = new List<StatusWithMessage>();

            if (!purgeCacheOn)
            {
                var cloudflareDisabled = new StatusWithMessage(false, ApplicationConstants.CloudflareMessages.CloudflareDisabled);
                results.Add(cloudflareDisabled);

                return results;
            }

            urls = new List<string>();// _umbracoFlareDomainService.FilterToAllowedDomains(urls);

            var groupings = urls.GroupBy(url => UrlHelper.GetDomainFromUrl(url, true));

            foreach (var domainUrlGroup in groupings)
            {
                //get the domain without the scheme or port.
                var domain = new UriBuilder(domainUrlGroup.Key).Uri;

                //Get the zone for the current website as configured by the "zoneUrl" config setting in the web.config.
                var websiteZone = GetZone(domain.DnsSafeHost);

                if (websiteZone == null)
                {
                    results.Add(new StatusWithMessage(false, $"Could not retrieve the zone from cloudflare with the domain(url) of {domain}"));

                    continue;
                }

                var apiResult = _cloudflareApiClient.PurgeCache(websiteZone.Id, domainUrlGroup);

                if (!apiResult)
                {
                    results.Add(new StatusWithMessage(false, ApplicationConstants.CloudflareMessages.CloudflareApiError));
                }
                else
                {
                    results.AddRange(domainUrlGroup.Select(url => new StatusWithMessage(true, $"Purged for url {url}")));
                }
            }

            return results;
        }

        public StatusWithMessage PurgeEverything(string domain)
        {
            //We only want the host and not the scheme or port number so just to ensure that is what we are getting we will proccess it as a uri.
            try
            {
                var domainAsUri = new Uri(domain);
                domain = domainAsUri.Authority;
            }
            catch (Exception)
            {
                //TODO: THIS LOGIC IS WRONG, CHECK CALLERS
                //So if we are here it didn't parse as an uri so we will assume that it was given in the correct format (without http://)
            }

            //Get the zone for the given domain
            var websiteZone = GetZone(domain);

            if (websiteZone == null)
            {
                return new StatusWithMessage(
                    false,
                    $"We could not purge the cache because the domain {domain} is not valid with the provided credentials. Please ensure this domain is registered under these credentials on your cloudflare dashboard.");
            }

            var purgeCacheStatus = _cloudflareApiClient.PurgeCache(websiteZone.Id, null, true);

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

            return statusMessages.ToString();
        }

        public UserDetails GetCloudflareUserDetails(CloudflareConfigModel configurationFile)
        {
            return _cloudflareApiClient.GetUserDetails(configurationFile);
        }

        private Zone GetZone(string url)
        {
            var zones = new List<string>(); //_umbracoFlareDomainService.GetAllowedCloudflareZones().Where(x => url.Contains(x.Name));

            //TODO:THIS DOESNT MAKE SENSE CHECK LATER
            //if (!zones.HasAny()) return zones.First();

            var noZoneException = new Exception($"Could not retrieve the zone from cloudflare with the domain(url) of {url}");
            _umbracoLoggingService.LogError<ICloudflareService>(noZoneException.Message, noZoneException);

            return null;
        }
    }
}