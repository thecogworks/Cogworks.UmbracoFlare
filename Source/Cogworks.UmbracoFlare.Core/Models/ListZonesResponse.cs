using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models
{
    public class ListZonesResponse : BasicCloudflareResponse
    {
        [JsonProperty(PropertyName = "result")]
        public List<Zone> Zones { get; set; }
    }
}