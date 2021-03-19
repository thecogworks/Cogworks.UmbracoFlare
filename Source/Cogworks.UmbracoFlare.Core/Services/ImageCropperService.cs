using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Models.CropModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Cogworks.UmbracoFlare.Core.Constants;
using Umbraco.Core.PropertyEditors;
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

        public ImageCropperService(IDataTypeService dataTypeService)
        {
            _dataTypeService = dataTypeService;
        }

        public IEnumerable<Crop> GetAllCrops()
        {
            var allCrops = new List<Crop>();
            var imageCropperDataTypes = _dataTypeService.GetByEditorAlias(ApplicationConstants.ImageCropperPropertyEditorAlias);

            foreach (var dataType in imageCropperDataTypes)
            {
                var valueList = (ImageCropperConfiguration)dataType.Configuration;

                //var preValues = new List<PreValue>();

                //if (valueList != null && valueList.Items.HasAny())
                //{
                //    preValues.AddRange(valueList.Items.Select(s => new PreValue
                //    {
                //        Id = s.Id,
                //        Value = s.Value
                //    }));
                //}
                
                //if (!preValues.HasAny()) { continue; }
                
                //var cropsFromDb = JsonConvert.DeserializeObject<IEnumerable<Crop>>(valueList);
                //allCrops.AddRange(cropsFromDb);
            }

            return allCrops;
        }
    }
    public class PreValue
    {
        public int Id { get; set; }

        public string Value { get; set; }
    }
}