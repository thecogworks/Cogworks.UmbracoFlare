namespace Cogworks.UmbracoFlare.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool HasValue(this string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }
    }
}