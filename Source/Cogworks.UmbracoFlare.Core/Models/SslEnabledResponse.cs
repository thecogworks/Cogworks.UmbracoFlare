using System;
using Newtonsoft.Json;

namespace Cogworks.UmbracoFlare.Core.Models
{
    public class SslEnabledResponse : BasicCloudflareResponse
    {
        [JsonProperty(PropertyName="result")]
        public SslEnabledSettings Result { get; set; }
    }

    public class SslEnabledSettings
    {
        [JsonProperty(PropertyName="id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName="value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName="Editable")]
        public string Editable{ get; set; }

        [JsonProperty(PropertyName="modified_on")]
        public DateTime ModifiedOn { get; set; }
    }
}
