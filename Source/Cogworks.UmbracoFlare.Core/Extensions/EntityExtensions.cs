using Umbraco.Core.Models.Entities;

namespace Cogworks.UmbracoFlare.Core.Extensions
{
    public static class EntityExtensions
    {
        public static bool IsNew(this IEntity entity)
        {
            return entity.CreateDate == entity.UpdateDate;
        }
    }
}