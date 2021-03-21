using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models.Cloudflare;
using Cogworks.UmbracoFlare.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Cogworks.UmbracoFlare.Core.Client
{
    public interface ICloudflareApiClient
    {
        UserDetails GetUserDetails();

        IEnumerable<Zone> GetZones();

        bool PurgeCache(string zoneId, IEnumerable<string> urls, bool purgeEverything);
    }

    public class CloudflareApiClient : ICloudflareApiClient
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;
        private readonly IConfigurationService _configurationService;


        public CloudflareApiClient(IUmbracoLoggingService umbracoLoggingService, IConfigurationService configurationService)
        {
            _umbracoLoggingService = umbracoLoggingService;
            _configurationService = configurationService;
        }

        public UserDetails GetUserDetails()
        {
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();
            var userDetails = new UserDetails();

            if (!umbracoFlareConfigModel.ApiKey.HasValue() || !umbracoFlareConfigModel.AccountEmail.HasValue())
            {
                return userDetails;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationConstants.ContentTypeApplicationJson));
                var url = $"{ApplicationConstants.CloudflareApi.BaseUrl}{ApplicationConstants.CloudflareApi.UserEndpoint}";
                
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                AddRequestHeaders(request);

                try
                {
                    var responseContent = client.SendAsync(request).Result.Content;
                    userDetails = responseContent.ReadAsAsync<UserDetails>().Result;

                    if (userDetails.Success) { return userDetails; }
                }
                catch (Exception e)
                {
                    _umbracoLoggingService.LogError<ICloudflareApiClient>($"Could not get the user details for user email {umbracoFlareConfigModel.AccountEmail}", e);
                    return userDetails;
                }

                _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"The request for <<GetUserDetails>> was not successful for user email {umbracoFlareConfigModel.AccountEmail}");
                return userDetails;
            }
        }

        public IEnumerable<Zone> GetZones()
        {
            using (var client = new HttpClient())
            {
                var url = $"{ApplicationConstants.CloudflareApi.BaseUrl}{ApplicationConstants.CloudflareApi.ZonesEndpoint}";
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationConstants.ContentTypeApplicationJson));

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                };

                AddRequestHeaders(request);

                try
                {
                    var responseContent = client.SendAsync(request).Result.Content;
                    var response = responseContent.ReadAsAsync<ZonesResponse>().Result;

                    if (response.Success)
                    {
                        return response.Zones;
                    }

                    _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"Could not get the list of zones because of {response.Messages}");
                    return Enumerable.Empty<Zone>();
                }
                catch (Exception e)
                {
                    _umbracoLoggingService.LogError<ICloudflareApiClient>("Something went wrong with the request for getting the zones. Could not get the List of zones", e);
                    return Enumerable.Empty<Zone>();
                }
            }
        }

        public bool PurgeCache(string zoneId, IEnumerable<string> urls, bool purgeEverything)
        {
            if (!zoneId.HasValue())
            {
                var zoneIdentifierException = new ArgumentNullException(nameof(zoneId));
                _umbracoLoggingService.LogError<ICloudflareApiClient>("The zone Identifier is empty", zoneIdentifierException);
                return false;
            }

            using (var client = new HttpClient())
            {
                var json = purgeEverything ? "{\"purge_everything\":true}" : $"{{\"files\":{JsonConvert.SerializeObject(urls)}}}";
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationConstants.ContentTypeApplicationJson));

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{ApplicationConstants.CloudflareApi.BaseUrl}zones/{zoneId}/purge_cache"),
                    Method = HttpMethod.Delete,
                    Content = new StringContent(json, Encoding.UTF8, ApplicationConstants.ContentTypeApplicationJson)
                };

                AddRequestHeaders(request);

                var responseContent = client.SendAsync(request).Result.Content;
                var stringVersion = responseContent.ReadAsStringAsync().Result;

                try
                {
                    var response = responseContent.ReadAsAsync<BasicCloudflareResponse>().Result;

                    if (!response.Success)
                    {
                        _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"Something went wrong because of {response.Messages}");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _umbracoLoggingService.LogError<ICloudflareApiClient>($"Something went wrong purging the cache. The url that was used is {request.RequestUri}. The json that was used is {json}. The raw string value is {stringVersion}", e);
                    return false;
                }

                return true;
            }
        }

        private void AddRequestHeaders(HttpRequestMessage request)
        {
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();

            request.Headers.Add("X-Auth-Key", umbracoFlareConfigModel.ApiKey);
            request.Headers.Add("X-Auth-Email", umbracoFlareConfigModel.AccountEmail);
        }
    }
}