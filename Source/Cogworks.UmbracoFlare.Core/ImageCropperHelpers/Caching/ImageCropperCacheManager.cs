using System;
using System.Web;

namespace Cogworks.UmbracoFlare.Core.ImageCropperHelpers.Caching
{
    public class ImageCropperCacheManager
    {
        private static ImageCropperCacheManager _instance;

        private ImageCropperCacheManager()
        {
        }

        public static ImageCropperCacheManager Instance => _instance ?? (_instance = new ImageCropperCacheManager());

        public T GetFromCache<T>(string cacheKey)
        {
            if (HttpRuntime.Cache[cacheKey] == null)
            {
                return default;
            }

            return (T)HttpRuntime.Cache[cacheKey];
        }

        public bool UpdateCache(string key, object value)
        {
            try
            {
                if (HttpRuntime.Cache[key] == null)
                {
                    HttpRuntime.Cache.Add(key, value, null, DateTime.Now.AddDays(1), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
                }
                else
                {
                    HttpRuntime.Cache[key] = value;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public static class ImageCropperCacheKeys
    {
        public static string AllCrops = "allCrops";
        public static string ImageCropperDataTypes = "imageCropperDataTypes";
    }
}