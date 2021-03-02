using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models
{
    public class PurgeCacheForUrlsRequestModel
    {
        public IEnumerable<string> Urls { get; set; }
        public IEnumerable<string> Domains { get; set; }
    }
}
