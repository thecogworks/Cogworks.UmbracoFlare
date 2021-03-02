using System.Collections.Generic;

namespace Cogworks.UmbracoFlare.Core.Models.CropModels
{
    public class UmbracoFile
    {
        public string Src { get; set; }
        public IEnumerable<Crop> Crops { get; set; }
        public FocalPoint FocalPoint { get; set; }
    }
}