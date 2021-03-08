using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;

namespace Cogworks.UmbracoFlare.Core.EventHandlers
{
    public class UmbracoFlareStartup : ApplicationEventHandler
    {
        protected override void ApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            TreeControllerBase.MenuRendering += AddPurgeCacheForContentMenu;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        private static void AddPurgeCacheForContentMenu(TreeControllerBase sender, MenuRenderingEventArgs args)
        {
            if (sender.TreeAlias != "content") { return; }

            var menuItem = new MenuItem("purgeCache", "Purge Cloudflare Cache")
            {
                Icon = "umbracoflare-tiny"
            };

            menuItem.LaunchDialogView("/App_Plugins/UmbracoFlare/dashboard/views/cogworks.umbracoflare.menu.html", "Purge Cloudflare Cache");

            args.Menu.Items.Insert(args.Menu.Items.Count - 1, menuItem);
        }
    }
}