using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cogworks.UmbracoFlare.Core.Models.Cloudflare
{
    public class ZonesResponse : BasicCloudflareResponse
    {
        [JsonProperty(PropertyName = "result")]
        public IEnumerable<Zone> Zones { get; set; }
    }
}