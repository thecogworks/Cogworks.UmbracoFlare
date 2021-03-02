namespace Cogworks.UmbracoFlare.Core.Models.Api
{
    public class PurgeCacheForIdParams
    {
        public int NodeId { get; set; }
        public bool PurgeChildren { get; set; }
    }
}