using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cogworks.UmbracoFlare.Core.Models.Cloudflare
{
    public class Zone
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName="development_mode")]
        public bool DevelopmentMode { get; set; }

        [JsonProperty(PropertyName="name_servers")]
        public List<string> NameServers { get; set; }
    }
}
