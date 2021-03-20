using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models.Api
{
    public class PurgeStaticFilesRequestModel
    {
        public IEnumerable<string> StaticFiles { get; set; }
        public string CurrentDomain { get; set; }
    }
}
