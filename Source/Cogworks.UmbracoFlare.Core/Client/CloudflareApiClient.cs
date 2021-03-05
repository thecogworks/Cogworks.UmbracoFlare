using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Cogworks.UmbracoFlare.Core.Constants;

namespace Cogworks.UmbracoFlare.Core.Client
{
    public interface ICloudflareApiClient
    {
        UserDetails GetUserDetails(CloudflareConfigModel configurationFile);

        IEnumerable<Zone> GetZones();

        bool PurgeCache(string zoneId, IEnumerable<string> urls, bool purgeEverything);
    }

    public class CloudflareApiClient : ICloudflareApiClient
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;

        public const string CloudflareApiBaseUrl = "https://api.cloudflare.com/client/v4/";
        public const string CloudflareApiUserEndpoint = "user";
        public const string CloudflareApiZonesEndpoint = "zones";
        private static string _apiKey;
        private static string _accountEmail;

        public CloudflareApiClient(IUmbracoLoggingService umbracoLoggingService)
        {
            _umbracoLoggingService = umbracoLoggingService;
        }

        public UserDetails GetUserDetails(CloudflareConfigModel configurationFile)
        {
            var userDetails = new UserDetails();

            if (!configurationFile.ApiKey.HasValue() || !configurationFile.AccountEmail.HasValue())
            {
                return userDetails;
            }

            _apiKey = configurationFile.ApiKey;
            _accountEmail = configurationFile.AccountEmail;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationConstants.ContentTypeApplicationJson));
                const string url = CloudflareApiBaseUrl + CloudflareApiUserEndpoint;

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
                    _umbracoLoggingService.LogError<ICloudflareApiClient>($"Could not get the user details for user email {_accountEmail}", e);
                    return userDetails;
                }

                _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"The request for <<GetUserDetails>> was not successful for user email {_accountEmail}");

                return userDetails;
            }
        }

        public IEnumerable<Zone> GetZones()
        {
            using (var client = new HttpClient())
            {
                var url = $"{CloudflareApiBaseUrl}{CloudflareApiZonesEndpoint}";
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
                    var response = responseContent.ReadAsAsync<ListZonesResponse>().Result;

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
                    RequestUri = new Uri(CloudflareApiBaseUrl + "zones/" + zoneId + "/purge_cache"),
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

        private static void AddRequestHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("X-Auth-Key", _apiKey);
            request.Headers.Add("X-Auth-Email", _accountEmail);
        }
    }
}