using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models.CropModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Factories;
using Umbraco.Core.Services;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IImageCropperService
    {
        IEnumerable<Crop> GetAllCrops();
    }

    public class ImageCropperService : IImageCropperService
    {
        private readonly IDataTypeService _dataTypeService;

        public ImageCropperService()
        {
            _dataTypeService = ServiceFactory.GetDataTypeService();
        }

        public IEnumerable<Crop> GetAllCrops()
        {
            var allCrops = new List<Crop>();
            var imageCropperDataTypes = _dataTypeService.GetDataTypeDefinitionByPropertyEditorAlias(ApplicationConstants.ImageCropperPropertyEditorAlias);

            foreach (var dataType in imageCropperDataTypes)
            {
                var crops = _dataTypeService.GetPreValuesByDataTypeId(dataType.Id).ToList();
                if (!crops.HasAny()) { continue; }

                var cropsFromDb = JsonConvert.DeserializeObject<IEnumerable<Crop>>(crops.First());
                allCrops.AddRange(cropsFromDb);
            }

            return allCrops;
        }
    }
}