using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models.Api
{
    public class PurgeUrlsRequestModel
    {
        public IEnumerable<string> Urls { get; set; }
        public IEnumerable<string> Domains { get; set; }
    }
}
