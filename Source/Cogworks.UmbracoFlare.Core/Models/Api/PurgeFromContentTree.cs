namespace Cogworks.UmbracoFlare.Core.Models.Api
{
    public class PurgeFromContentTree
    {
        public int NodeId { get; set; }
        public bool PurgeChildren { get; set; }
        public string CurrentDomain { get; set; }
    }
}