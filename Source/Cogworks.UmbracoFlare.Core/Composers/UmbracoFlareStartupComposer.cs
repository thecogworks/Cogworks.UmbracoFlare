using Cogworks.UmbracoFlare.Core.Client;
using Cogworks.UmbracoFlare.Core.Components;
using Cogworks.UmbracoFlare.Core.Services;
using LightInject;
using System;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Cogworks.UmbracoFlare.Core.Composers
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoFlareStartupComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            var container = composition.Concrete as ServiceContainer;
            RegisterServices(container);
            RegisterAssembliesControllers(container);

            composition.Components()
                .Append<UmbracoFlareStartupComponent>()
                .Append<UmbracoFlareEventsComponent>();
        }

        private static void RegisterServices(IServiceRegistry container)
        {
            container.Register<IUmbracoFlareDomainService, UmbracoFlareDomainService>();
            container.Register<IUmbracoLoggingService, UmbracoLoggingService>();
            container.Register<ICloudflareService, CloudflareService>();
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