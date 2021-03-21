using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;

namespace Cogworks.UmbracoFlare.Core.Services
{
    public interface IImageCropperService
    {
        IEnumerable<ImageCropperConfiguration.Crop> GetAllCrops();
    }

    public class ImageCropperService : IImageCropperService
    {
        private readonly IDataTypeService _dataTypeService;

        public ImageCropperService(IDataTypeService dataTypeService)
        {
            _dataTypeService = dataTypeService;
        }

        public IEnumerable<ImageCropperConfiguration.Crop> GetAllCrops()
        {
            var allCrops = new List<ImageCropperConfiguration.Crop>();
            var imageCropperDataTypes = _dataTypeService.GetByEditorAlias(ApplicationConstants.ImageCropperPropertyEditorAlias);

            foreach (var dataType in imageCropperDataTypes)
            {
                var valueList = (ImageCropperConfiguration)dataType.Configuration;
                var crops = valueList?.Crops?.ToList();

                if (!crops.HasAny()) { continue; }

                allCrops.AddRange(crops);
            }

            return allCrops;
        }
    }
}