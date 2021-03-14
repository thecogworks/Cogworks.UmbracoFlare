namespace Cogworks.UmbracoFlare.Core.Models.Api
{
    public class PurgeStaticFilesRequestModel
    {
        public string[] StaticFiles { get; set; }
        public string CurrentDomain { get; set; }
    }
}
