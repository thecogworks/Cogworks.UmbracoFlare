using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Services;
using Cogworks.UmbracoFlare.Core.Wrappers;
using LightInject;
using LightInject.Mvc;
using LightInject.WebApi;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;

namespace Cogworks.UmbracoFlare.Core
{
    public static class IoCBootstrapper
    {
        public static void IoCSetup()
        {
            var container = new ServiceContainer();

            RegisterCoreUmbracoServices(container);

            RegisterServices(container);
            RegisterAssembliesControllers(container);

            container.EnableMvc();
            container.EnablePerWebRequestScope();
            container.EnableWebApi(GlobalConfiguration.Configuration);

            var resolver = new LightInjectWebApiDependencyResolver(container);

            GlobalConfiguration.Configuration.DependencyResolver = resolver;
            DependencyResolver.SetResolver(new LightInjectMvcDependencyResolver(container));
        }

        private static void RegisterCoreUmbracoServices(IServiceRegistry container)
        {
            container.Register<IUmbracoContextWrapper, UmbracoContextWrapper>().RegisterInstance(new PerScopeLifetime());
            container.Register<IUmbracoHelperWrapper, UmbracoHelperWrapper>().RegisterInstance(new PerScopeLifetime());
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
        }

        private static void RegisterServices(IServiceRegistry container)
        {
            container.Register<IUmbracoFlareDomainService, UmbracoFlareDomainService>();
            container.Register<ICloudflareService, CloudflareService>();
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
            container.Register<ICloudflareApiClient, CloudflareApiClient>();

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
    }
}