using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Services;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;

namespace Cogworks.UmbracoFlare.Core.Components
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoFlareEventsComponent : IComponent
    {
        private readonly IImageCropperService _imageCropperService;
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IConfigurationService _configurationService;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;
        private readonly ICloudflareService _cloudflareService;

        public UmbracoFlareEventsComponent(IImageCropperService imageCropperService, IUmbracoContextFactory umbracoContextFactory,
            IConfigurationService configurationService, IUmbracoFlareDomainService umbracoFlareDomainService, ICloudflareService cloudflareService)
        {
            _imageCropperService = imageCropperService;
            _umbracoContextFactory = umbracoContextFactory;
            _configurationService = configurationService;
            _umbracoFlareDomainService = umbracoFlareDomainService;
            _cloudflareService = cloudflareService;
        }

        public void Initialize()
        {
            ContentService.Published += PurgeCloudflareCache;
            FileService.SavedScript += PurgeCloudflareCacheForScripts;
            FileService.SavedStylesheet += PurgeCloudflareCacheForStylesheets;
            MediaService.Saved += PurgeCloudflareCacheForMedia;

            TreeControllerBase.MenuRendering += AddPurgeCacheForContentMenu;
        }

        private void PurgeCloudflareCache(IContentService sender, ContentPublishedEventArgs e)
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

        public void Terminate()
        {
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
                if (file.IsNew()) { continue; }
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

            using (var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext())
            {
                var mediaCache = umbracoContextReference.UmbracoContext.Media;

                foreach (var media in e.SavedEntities)
                {
                    if (media.IsNew() || media.GetValue<bool>(ApplicationConstants.UmbracoFlareBackendProperties.CloudflareDisabledOnPublishPropertyAlias)) { continue; }

                    var publishedMedia = mediaCache.GetById(media.Id);

                    if (publishedMedia == null)
                    {
                        e.Messages.Add(new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching, "We could not find the IPublishedContent version of the media: " + media.Id + " you are trying to save.", EventMessageType.Error));
                        continue;
                    }

                    urls.AddRange(imageCropSizes.Select(x => publishedMedia.GetCropUrl(x.Alias)));
                    urls.Add(publishedMedia.Url());
                }
            }

            var fullUrls = UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(urls, currentDomain, true);
            var result = _cloudflareService.PurgePages(fullUrls);

            e.Messages.Add(result.Success
                ? new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "Successfully purged the cloudflare cache.", EventMessageType.Success)
                : new EventMessage(ApplicationConstants.EventMessageCategory.CloudflareCaching,
                    "We could not purge the Cloudflare cache.", EventMessageType.Warning));
        }

        private static void AddPurgeCacheForContentMenu(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            if (sender.TreeAlias != "content") { return; }

            MenuItem menuItem;

            if (e.NodeId == "-1") // if content root
            {
                menuItem = new MenuItem("purgeCache", "Purge Cloudflare Cache")
                {
                    Icon = "umbracoflare-tiny"
                };

                menuItem.LaunchDialogView("/App_Plugins/UmbracoFlare/dashboard/views/cogworks.umbracoflare.rootmenu.html", "Purge Cloudflare Cache");
            }
            else
            {
                menuItem = new MenuItem("purgeCache", "Purge Cloudflare Cache")
                {
                    Icon = "umbracoflare-tiny"
                };

                menuItem.LaunchDialogView("/App_Plugins/UmbracoFlare/dashboard/views/cogworks.umbracoflare.menu.html", "Purge Cloudflare Cache");
            }

            e.Menu.Items.Insert(e.Menu.Items.Count - 1, menuItem);
        }
    }
}