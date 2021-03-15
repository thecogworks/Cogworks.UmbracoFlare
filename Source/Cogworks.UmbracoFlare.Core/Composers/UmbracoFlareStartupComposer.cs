using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Services;
using LightInject;
using System;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using Cogworks.UmbracoFlare.Core.Components;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Cogworks.UmbracoFlare.Core.Composers
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoFlareStartupComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<UmbracoFlareStartupComponent>();

            var container = composition.Concrete as ServiceContainer;

            RegisterCoreUmbracoServices(container);
            RegisterServices(container);
            RegisterAssembliesControllers(container);
        }

        private static void RegisterCoreUmbracoServices(IServiceRegistry container)
        {
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
        }

        private static void RegisterServices(IServiceRegistry container)
        {
            container.Register<IUmbracoFlareDomainService, UmbracoFlareDomainService>();
            container.Register<ICloudflareService, CloudflareService>();
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
            container.Register<ICloudflareApiClient, CloudflareApiClient>();
            container.Register<IConfigurationService, ConfigurationService>();
            container.Register<IImageCropperService, ImageCropperService>();
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