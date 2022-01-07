namespace Website.ViewModels
{
    using Microsoft.AspNetCore.Html;
    using Umbraco.Cms.Core.Models.PublishedContent;
    using Umbraco.Cms.Web.Common.PublishedModels;

    public class ControllersViewModel : Controller
    {
        public ControllersViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback) : base(content, publishedValueFallback)
        {
        }

        public HtmlString PictureSrc { get; set; }
        public HtmlString PictureSrcSet { get; set; }
    }
}
