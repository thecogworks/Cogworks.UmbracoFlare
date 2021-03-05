using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.ImageCropperHelpers;
using Cogworks.UmbracoFlare.Core.Services;
using Cogworks.UmbracoFlare.Core.Wrappers;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web;
using UrlHelper = Cogworks.UmbracoFlare.Core.Helpers.UrlHelper;

namespace Cogworks.UmbracoFlare.Core.EventHandlers
{
    public class UmbracoFlareEvents : ApplicationEventHandler
    {
        private ICloudflareService _cloudflareService;
        private IUmbracoFlareDomainService _umbracoFlareDomainService;
        private IUmbracoHelperWrapper _umbracoHelperWrapper;
        private static bool _purgeCacheOn;

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            _purgeCacheOn = DependencyResolver.Current.GetService<IConfigurationService>().LoadConfigurationFile().PurgeCacheOn;
            _cloudflareService = DependencyResolver.Current.GetService<ICloudflareService>();
            _umbracoFlareDomainService = DependencyResolver.Current.GetService<IUmbracoFlareDomainService>();
            _umbracoHelperWrapper = DependencyResolver.Current.GetService<IUmbracoHelperWrapper>();

            ContentService.Published += PurgeCloudflareCache;
            
            FileService.SavedScript += PurgeCloudflareCacheForScripts;
            FileService.SavedStylesheet += PurgeCloudflareCacheForStylesheets;

            MediaService.Saved += PurgeCloudflareCacheForMedia;
            DataTypeService.Saved += RefreshImageCropsCache;
        }

        protected void PurgeCloudflareCache(IPublishingStrategy strategy, PublishEventArgs<IContent> e)
        {
            if (!_purgeCacheOn) { return; }

            var urls = new List<string>();

            foreach (var content in e.PublishedEntities)
            {
                if (content.GetValue<bool>(ApplicationConstants.UmbracoFlareBackendProperties.CloudflareDisabledOnPublishPropertyAlias)) { continue; }

                urls.AddRange(_umbracoFlareDomainService.GetUrlsForNode(content.Id));
            }

            var results = _cloudflareService.PurgePages(urls);

            if (results.HasAny())
            {
                e.Messages.Add(results.Any(x => !x.Success)
                    ? new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                        "We could not purge the Cloudflare cache. \n \n" +
                        _cloudflareService.PrintResultsSummary(results), EventMessageType.Warning)
                    : new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching, "Successfully purged the cloudflare cache.",
                        EventMessageType.Success));
            }
        }

        protected void UpdateContentIdToUrlCache(IPublishingStrategy strategy, PublishEventArgs<IContent> e)
        {
            foreach (var content in e.PublishedEntities)
            {
                if (content.HasPublishedVersion)
                {
                    var urls = _umbracoFlareDomainService.GetUrlsForNode(content.Id);

                    if (urls.Contains("#"))
                    {
                        //When a piece of content is first saved, we cannot get the url, if that is the case then we need to just
                        //invalidate the who ContentIdToUrlCache, that way when we request all of the urls agian, it will pick it up.
                        _umbracoFlareDomainService.DeletedContentIdToUrlCache();
                    }
                    else
                    {
                        _umbracoFlareDomainService.UpdateContentIdToUrlCache(content.Id, urls);
                    }
                }

                //TODO: Does this need to be here?
                //We also need to update the descendants now because their urls changed
                var descendants = content.Descendants();

                foreach (var descendant in descendants)
                {
                    var descUrls = _umbracoFlareDomainService.GetUrlsForNode(descendant.Id);

                    _umbracoFlareDomainService.UpdateContentIdToUrlCache(content.Id, descUrls);
                }
            }
        }

        private void PurgeCloudflareCacheForScripts(IFileService sender, SaveEventArgs<Script> e)
        {
            var files = e.SavedEntities.Select(script => script as File);
            PurgeCloudflareCacheForFiles(files, e);
        }

        private void PurgeCloudflareCacheForStylesheets(IFileService sender, SaveEventArgs<Stylesheet> e)
        {
            var files = e.SavedEntities.Select(stylesheet => stylesheet as File);
            PurgeCloudflareCacheForFiles(files, e);
        }

        private void PurgeCloudflareCacheForFiles<T>(IEnumerable<File> files, SaveEventArgs<T> e)
        {
            if (!_purgeCacheOn) { return; }

            var urls = new List<string>();

            //GetUmbracoDomains
            var domains = _umbracoFlareDomainService.GetAllowedCloudflareDomains();

            foreach (var file in files)
            {
                if (file.IsNewEntity())
                {
                    //If its new we don't want to purge the cache as this causes slow upload times.
                    continue;
                }

                urls.Add(file.VirtualPath);
            }

            var results = _cloudflareService.PurgePages(UrlHelper.GetFullUrlForPurgeEvents(urls, domains, true));

            if (results.HasAny() && results.Any(x => !x.Success))
            {
                e.Messages.Add(new EventMessage("Cloudflare Caching", "We could not purge the Cloudflare cache. \n \n" + _cloudflareService.PrintResultsSummary(results), EventMessageType.Warning));
            }
            else if (results.Any())
            {
                e.Messages.Add(new EventMessage("Cloudflare Caching", "Successfully purged the cloudflare cache.", EventMessageType.Success));
            }
        }

        protected void PurgeCloudflareCacheForMedia(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            //WORKING HERE
            if (!_purgeCacheOn) { return; }

            var imageCropSizes = ImageCropperManager.Instance.GetAllCrops();
            var urls = new List<string>();
            var allowedCloudflareDomains = _umbracoFlareDomainService.GetAllowedCloudflareDomains();

            foreach (var media in e.SavedEntities)
            {
                if (media.IsNewEntity() || media.GetValue<bool>(ApplicationConstants.UmbracoFlareBackendProperties.CloudflareDisabledOnPublishPropertyAlias)) { continue; }

                var publishedMedia = _umbracoHelperWrapper.TypedMedia(media.Id);

                if (publishedMedia == null)
                {
                    e.Messages.Add(new EventMessage("Cloudflare Caching", "We could not find the IPublishedContent version of the media: " + media.Id + " you are trying to save.", EventMessageType.Error));
                    continue;
                }

                urls.AddRange(imageCropSizes.Select(crop => publishedMedia.GetCropUrl(crop.Alias)));
                urls.Add(publishedMedia.Url);
            }

            var fullUrls = UrlHelper.GetFullUrlForPurgeEvents(urls, allowedCloudflareDomains, true);
            var results = _cloudflareService.PurgePages(fullUrls);

            if (results.HasAny() && results.Any(x => !x.Success))
            {
                e.Messages.Add(new EventMessage("Cloudflare Caching", "We could not purge the Cloudflare cache. \n \n" + _cloudflareService.PrintResultsSummary(results), EventMessageType.Warning));
            }
            else if (results.Any())
            {
                e.Messages.Add(new EventMessage("Cloudflare Caching", "Successfully purged the cloudflare cache.", EventMessageType.Success));
            }
        }

        protected void RefreshImageCropsCache(IDataTypeService sender, SaveEventArgs<IDataTypeDefinition> e)
        {
            //A data type has saved, see if it was a
            var imageCroppers = ImageCropperManager.Instance.GetImageCropperDataTypes(true);
            var freshlySavedImageCropper = imageCroppers.Intersect(e.SavedEntities);

            if (imageCroppers.Intersect(e.SavedEntities).Any())
            {
                //There were some freshly saved Image cropper data types so refresh the image crop cache.
                //We can do that by simply getting the crops
                ImageCropperManager.Instance.GetAllCrops(true); //true to bypass the cache & refresh it.
            }
        }
    }
}