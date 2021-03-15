using Cogworks.UmbracoFlare.Core.Constants;
using Cogworks.UmbracoFlare.Core.Extensions;
using Cogworks.UmbracoFlare.Core.Factories;
using Cogworks.UmbracoFlare.Core.Helpers;
using Cogworks.UmbracoFlare.Core.Services;
using System.Collections.Generic;
using System.Linq;
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
    public class UmbracoFlareEventsComponent : IComponent
    {
        private readonly ICloudflareService _cloudflareService;
        private readonly IUmbracoFlareDomainService _umbracoFlareDomainService;
        private readonly UmbracoHelper _umbracoHelper;
        private readonly IImageCropperService _imageCropperService;
        private readonly IConfigurationService _configurationService;

        public UmbracoFlareEventsComponent(UmbracoHelper umbracoHelper)
        {
            _umbracoHelper = umbracoHelper;
            _configurationService = ServiceFactory.GetConfigurationService();
            _cloudflareService = ServiceFactory.GetCloudflareService();
            _umbracoFlareDomainService = ServiceFactory.GetUmbracoFlareDomainService();
            _imageCropperService = ServiceFactory.GetImageCropperService();

            TreeControllerBase.MenuRendering += AddPurgeCacheForContentMenu;
        }

        public void Initialize()
        {
            ContentService.Published += PurgeCloudflareCache;
            FileService.SavedScript += PurgeCloudflareCacheForScripts;
            FileService.SavedStylesheet += PurgeCloudflareCacheForStylesheets;
            MediaService.Saved += PurgeCloudflareCacheForMedia;
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
                if (file.HasIdentity) { continue; }
                urls.Add(file.VirtualPath);
            }

            var fullUrls = UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(urls, currentDomain, true);

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

        protected void PurgeCloudflareCacheForMedia(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            var umbracoFlareConfigModel = _configurationService.LoadConfigurationFile();
            if (!umbracoFlareConfigModel.PurgeCacheOn) { return; }

            var imageCropSizes = _imageCropperService.GetAllCrops().ToList();
            var urls = new List<string>();

            var currentDomain = UmbracoFlareUrlHelper.GetCurrentDomain();

            foreach (var media in e.SavedEntities)
            {
                if (media.HasIdentity || media.GetValue<bool>(ApplicationConstants.UmbracoFlareBackendProperties.CloudflareDisabledOnPublishPropertyAlias)) { continue; }

                var publishedMedia = _umbracoHelper.Media(media.Id);
                
                if (publishedMedia == null)
                {
                    e.Messages.Add(new EventMessage("Cloudflare Caching", "We could not find the IPublishedContent version of the media: " + media.Id + " you are trying to save.", EventMessageType.Error));
                    continue;
                }

                urls.AddRange(imageCropSizes.Select(x=> publishedMedia.GetCropUrl(x.Alias)));
                urls.Add(publishedMedia.Url());
            }

            var fullUrls = UmbracoFlareUrlHelper.MakeFullUrlsWithDomain(urls, currentDomain, true);
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

        private static void AddPurgeCacheForContentMenu(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            if (sender.TreeAlias != "content") { return; }

            var menuItem = new MenuItem("purgeCache", "Purge Cloudflare Cache")
            {
                Icon = "umbracoflare-tiny"
            };

            menuItem.LaunchDialogView("/App_Plugins/UmbracoFlare/dashboard/views/cogworks.umbracoflare.menu.html", "Purge Cloudflare Cache");

            e.Menu.Items.Insert(e.Menu.Items.Count - 1, menuItem);
        }
    }
}