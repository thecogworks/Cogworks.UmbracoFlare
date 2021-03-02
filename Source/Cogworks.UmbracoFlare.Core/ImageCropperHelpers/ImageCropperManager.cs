using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.ImageCropperHelpers.Caching;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Cogworks.UmbracoFlare.Core.Models.CropModels;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Cogworks.UmbracoFlare.Core.ImageCropperHelpers
{
    public class ImageCropperManager
    {
        private static ImageCropperManager _instance;
        //public IDataTypeService DataTypeService => ApplicationContext.Current.Services.DataTypeService;

        //private ImageCropperManager()
        //{
        //}

        public static ImageCropperManager Instance => _instance ?? (_instance = new ImageCropperManager());

        public List<Crop> GetAllCrops(bool bypassCache = false)
        {
            List<Crop> allCrops;

            if (bypassCache)
            {
                allCrops = GetAllCropsFromDataTypeServiceAndUpdateCache();
            }
            else
            {
                allCrops = ImageCropperCacheManager.Instance.GetFromCache<List<Crop>>(ImageCropperCacheKeys.AllCrops)
                           ?? GetAllCropsFromDataTypeServiceAndUpdateCache();
            }

            return allCrops;
        }

        private List<Crop> GetAllCropsFromDataTypeServiceAndUpdateCache()
        {
            var allCrops = new List<Crop>();

            //We are bypassing the cache so we have to get everything again from the db.
            var imageCropperDataTypes = GetImageCropperDataTypes(true);

            foreach (var dataType in imageCropperDataTypes)
            {
                var cropsStr = ApplicationContext.Current.Services.DataTypeService.GetPreValuesByDataTypeId(dataType.Id);

                if (!cropsStr.HasAny()) { continue; }

                var cropsFromDb = JsonConvert.DeserializeObject<IEnumerable<Crop>>(cropsStr.First());
                allCrops.AddRange(cropsFromDb);
            }

            ImageCropperCacheManager.Instance.UpdateCache(ImageCropperCacheKeys.AllCrops, allCrops);

            return allCrops;
        }

        public IEnumerable<IDataTypeDefinition> GetImageCropperDataTypes(bool bypassCache = false)
        {
            IEnumerable<IDataTypeDefinition> imageCropperDataTypes;
            if (bypassCache)
            {
                imageCropperDataTypes = ApplicationContext.Current.Services.DataTypeService.GetDataTypeDefinitionByPropertyEditorAlias("Umbraco.ImageCropper");
                ImageCropperCacheManager.Instance.UpdateCache(ImageCropperCacheKeys.ImageCropperDataTypes, imageCropperDataTypes);

                return imageCropperDataTypes;
            }

            imageCropperDataTypes = ImageCropperCacheManager.Instance.GetFromCache<IEnumerable<IDataTypeDefinition>>(ImageCropperCacheKeys.ImageCropperDataTypes);

            if (imageCropperDataTypes != null) { return imageCropperDataTypes; }

            imageCropperDataTypes = ApplicationContext.Current.Services.DataTypeService.GetDataTypeDefinitionByPropertyEditorAlias("Umbraco.ImageCropper");
            ImageCropperCacheManager.Instance.UpdateCache(ImageCropperCacheKeys.ImageCropperDataTypes, imageCropperDataTypes);

            return imageCropperDataTypes;
        }
    }
}