using Cogworks.UmbracoFlare.Core.Components;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Cogworks.UmbracoFlare.Core.Composers
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoFlareEventsComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            //composition.Components().Append<UmbracoFlareEventsComponent>();
        }
    }
}