using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cogworks.UmbracoFlare.Core.Models
{
    public class BasicCloudflareResponse
    {
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "errors")]
        public List<CloudflareError> Errors { get; set; }

        [JsonProperty(PropertyName = "messages")]
        public List<string> Messages { get; set; }
    }

    public class CloudflareError
    {
        [JsonProperty(PropertyName="code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
