﻿using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Factories;
using Cogworks.UmbracoFlare.Core.Services;
using Cogworks.UmbracoFlare.Core.Wrappers;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Web;
using UmbracoFlareUrlHelper = Cogworks.UmbracoFlare.Core.Helpers.UmbracoFlareUrlHelper;

namespace Cogworks.UmbracoFlare.Core.EventHandlers
{
    public class UmbracoFlareEvents : ApplicationEventHandler
    {
        private ICloudflareService _cloudflareService;
        private IUmbracoFlareDomainService _umbracoFlareDomainService;
        private IUmbracoHelperWrapper _umbracoHelperWrapper;
        private IImageCropperService _imageCropperService;
        private IConfigurationService _configurationService;

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            _configurationService = ServiceFactory.GetConfigurationService();
            _cloudflareService = ServiceFactory.GetCloudflareService();
            _umbracoFlareDomainService = ServiceFactory.GetUmbracoFlareDomainService();
            _umbracoHelperWrapper = ServiceFactory.GetUmbracoHelperWrapper();
            _imageCropperService = ServiceFactory.GetImageCropperService();

            ContentService.Published += PurgeCloudflareCache;
            FileService.SavedScript += PurgeCloudflareCacheForScripts;
            FileService.SavedStylesheet += PurgeCloudflareCacheForStylesheets;
            MediaService.Saved += PurgeCloudflareCacheForMedia;
        }

        protected void PurgeCloudflareCache(IPublishingStrategy strategy, PublishEventArgs<IContent> e)
        {
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();
            if (!umbracoFlareConfigModel.PurgeCacheOn) { return; }

            var urls = new List<string>();
            var currentDomain = UmbracoFlareUrlHelper.GetCurrentDomain();

            foreach (var content in e.PublishedEntities)
            {
                if (content.GetValue<bool>(ApplicationConstants.UmbracoFlareBackendProperties.CloudflareDisabledOnPublishPropertyAlias)) { continue; }

                urls.AddRange(_umbracoFlareDomainService.GetUrlsForNode(content.Id, currentDomain));
            }

            var result = _cloudflareService.PurgePages(urls);

            e.Messages.Add(result.Success
                ? new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "Successfully purged the cloudflare cache.", EventMessageType.Success)
                : new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "We could not purge the Cloudflare cache. Please check the logs to find out more.",
                    EventMessageType.Warning));
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
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();
            if (!umbracoFlareConfigModel.PurgeCacheOn) { return; }
            if (!files.HasAny()) { return; }

            var currentDomain = UmbracoFlareUrlHelper.GetCurrentDomain();
            var urls = new List<string>();

            foreach (var file in files)
            {
                if (file.IsNewEntity()) { continue; }
                urls.Add(file.VirtualPath);
            }

            var fullUrls = UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(urls, currentDomain, true);

            var result = _cloudflareService.PurgePages(fullUrls);

            e.Messages.Add(result.Success
                ? new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching, "Successfully purged the cloudflare cache.",
                    EventMessageType.Success)
                : new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching, "We could not purge the Cloudflare cache.",
                    EventMessageType.Warning));
        }

        protected void PurgeCloudflareCacheForMedia(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();
            if (!umbracoFlareConfigModel.PurgeCacheOn) { return; }

            var imageCropSizes = _imageCropperService.GetAllCrops().ToList();
            var urls = new List<string>();

            var currentDomain = UmbracoFlareUrlHelper.GetCurrentDomain();

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

            var fullUrls = UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(urls, currentDomain, true);
            var result = _cloudflareService.PurgePages(fullUrls);

            e.Messages.Add(result.Success
                ? new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "Successfully purged the cloudflare cache.", EventMessageType.Success)
                : new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "We could not purge the Cloudflare cache.", EventMessageType.Warning));
        }
    }
}