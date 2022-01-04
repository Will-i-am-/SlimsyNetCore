namespace Slimsy
{
    using global::Slimsy.Configuration;
    using Microsoft.AspNetCore.Html;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.Extensions.Options;
    using Umbraco.Cms.Core.Models.PublishedContent;

    public static class Slimsy
    {
        //private static readonly IOptions<SlimsyConfiguration> _slimsyOptions;

        //public static Slimsy(IOptions<SlimsyConfiguration> slimsyOptions)
        //{
        //    _slimsyOptions = slimsyOptions;
        //}

        /// <summary>
        /// Generate SrcSet markup based on a width and height for the image cropped around the focal point
        /// </summary>
        /// <param name="urlHelper"></param>
        /// <param name="publishedContent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>HTML Markup</returns>

        public static HtmlString GetSrcSetUrls(this UrlHelper urlHelper, IPublishedContent publishedContent, int width, int height)
        {
            
            return null;
        }
    }
}
