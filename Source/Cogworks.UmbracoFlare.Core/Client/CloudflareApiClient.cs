using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models;
using Cogworks.UmbracoFlare.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace Cogworks.UmbracoFlare.Core.Client
{
    public interface ICloudflareApiClient
    {
        UserDetails GetUserDetails(CloudflareConfigModel configurationFile);

        SslEnabledResponse GetSslStatus(string zoneId);

        IEnumerable<Zone> GetZones(string name = null);

        bool PurgeCache(string zoneIdentifier, IEnumerable<string> urls, bool purgeEverything = false);
    }

    public class CloudflareApiClient : ICloudflareApiClient
    {
        private readonly IUmbracoLoggingService _umbracoLoggingService;

        public const string CloudflareApiBaseUrl = "https://api.cloudflare.com/client/v4/";
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                const string url = CloudflareApiBaseUrl + "user";

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

        public SslEnabledResponse GetSslStatus(string zoneId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(CloudflareApiBaseUrl + "zones/" + zoneId + "/settings/ssl"),
                    Method = HttpMethod.Get,
                };

                AddRequestHeaders(request);

                var responseContent = client.SendAsync(request).Result.Content;
                var stringVersion = responseContent.ReadAsStringAsync().Result;

                try
                {
                    var response = responseContent.ReadAsAsync<SslEnabledResponse>().Result;
                    if (response.Success) { return response; }

                    _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"Something went wrong because of {response.Messages}");

                    return null;
                }
                catch (Exception e)
                {
                    _umbracoLoggingService.LogError<ICloudflareApiClient>($"Something went wrong getting the SSL response back. The url that was used is {request.RequestUri}. ZoneId {zoneId}. The raw string value is {stringVersion}", e);
                    return null;
                }
            }
        }

        public IEnumerable<Zone> GetZones(string name = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = CloudflareApiBaseUrl + "zones";

                    if (name.HasValue())
                    {
                        url += "?name=" + HttpUtility.UrlEncode(name);
                    }

                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Get,
                    };

                    AddRequestHeaders(request);

                    var responseContent = client.SendAsync(request).Result.Content;

                    var response = responseContent.ReadAsAsync<ListZonesResponse>().Result;

                    if (response.Success)
                    {
                        return response.Zones;
                    }

                    _umbracoLoggingService.LogWarn<ICloudflareApiClient>($"Could not get the list of zones for name {name} because of {response.Messages}");
                    return new List<Zone>();
                }
            }
            catch (Exception e)
            {
                _umbracoLoggingService.LogError<ICloudflareApiClient>($"Could not get the List of zones for name {name}", e);

                return new List<Zone>();
            }
        }

        public bool PurgeCache(string zoneIdentifier, IEnumerable<string> urls, bool purgeEverything = false)
        {
            if (!zoneIdentifier.HasValue())
            {
                var zoneIdentifierException = new ArgumentNullException(nameof(zoneIdentifier));
                _umbracoLoggingService.LogError<ICloudflareApiClient>($"The parameter zoneIdentifier is empty", zoneIdentifierException);
                return false;
            }

            if (!urls.HasAny() && !purgeEverything)
            {
                _umbracoLoggingService.LogInfo<ICloudflareApiClient>("PurgeIndividualPages was called but there were no urls given to purge nor are we purging everything");
                return true;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var json = purgeEverything ? "{\"purge_everything\":true}" : $"{{\"files\":{JsonConvert.SerializeObject(urls)}}}";

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(CloudflareApiBaseUrl + "zones/" + zoneIdentifier + "/purge_cache"),
                    Method = HttpMethod.Delete,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
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