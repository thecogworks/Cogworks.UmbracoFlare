using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Services;
using Cogworks.UmbracoFlare.Core.Wrappers;
using LightInject;
using LightInject.Mvc;
using LightInject.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;
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

            var container = new ServiceContainer();

            RegisterCoreUmbracoServices(container, applicationContext);
            RegisterServices(container);
            RegisterAssembliesControllers(container);

            container.EnableMvc();
            container.EnablePerWebRequestScope();
            container.EnableWebApi(GlobalConfiguration.Configuration);

            var resolver = new LightInjectWebApiDependencyResolver(container);

            GlobalConfiguration.Configuration.DependencyResolver = resolver;
            DependencyResolver.SetResolver(new LightInjectMvcDependencyResolver(container));

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                //Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            TreeControllerBase.MenuRendering += AddPurgeCacheForContentMenu;
        }

        private static void RegisterCoreUmbracoServices(IServiceRegistry container, ApplicationContext applicationContext)
        {
            container.Register<IUmbracoContextWrapper, UmbracoContextWrapper>().RegisterInstance(new PerScopeLifetime());
            container.Register<IUmbracoHelperWrapper, UmbracoHelperWrapper>().RegisterInstance(new PerScopeLifetime());
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();

            container.RegisterInstance(applicationContext.Services.DomainService);
            container.RegisterInstance(applicationContext.Services.DataTypeService);
        }

        private static void RegisterServices(IServiceRegistry container)
        {
            container.Register<IUmbracoFlareDomainService, UmbracoFlareDomainService>();
            container.Register<ICloudflareService, CloudflareService>();
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
            container.Register<ICloudflareApiClient, CloudflareApiClient>();
            container.Register<IConfigurationService, ConfigurationService>();

            //if (ConfigurationSettings.IsCacheEnabled)
            //{
            //    container.Register<UmbracoContentNodeService, UmbracoContentNodeService>();

            //    container.Register<IUmbracoContentNodeService>(factory => new UmbracoContentNodeServiceCachedProxy(factory.GetInstance<UmbracoContentNodeService>(),
            //        factory.GetInstance<IUmbracoHelperWrapper>()));
            //}
            //else
            //{
            //    container.Register<IUmbracoContentNodeService, UmbracoContentNodeService>();
            //}
        }

        private static void RegisterAssembliesControllers(IServiceRegistry container)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var controllerTypes = assembly.GetTypes().Where(t => !t.IsAbstract && (typeof(IController).IsAssignableFrom(t) || typeof(IHttpController).IsAssignableFrom(t)));

                foreach (var controllerType in controllerTypes)
                {
                    container.Register(controllerType, new PerRequestLifeTime());
                }
            }
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