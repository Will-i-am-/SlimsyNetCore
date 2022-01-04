namespace Slimsy.Services
{
    using System.Text;
    using System.Web;
    using Microsoft.AspNetCore.Html;
    using Umbraco.Cms.Core;
    using Umbraco.Cms.Core.Models;
    using Umbraco.Cms.Core.Models.PublishedContent;
    using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
    using Umbraco.Cms.Core.Web;
    using Umbraco.Extensions;

    public class SlimsyService
    {
        private readonly ISlimsyOptions _slimsyOptions;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private static readonly HtmlString EmptyHtmlString = new HtmlString(string.Empty);

        public SlimsyService(ISlimsyOptions slimsyOptions, IUmbracoContextAccessor umbracoContextAccessor)
        {
            this._slimsyOptions = slimsyOptions;
            this._umbracoContextAccessor = umbracoContextAccessor;
        }

        public int GetWidthStep()
        {
            return _slimsyOptions.WidthStep;
        }

        /// <summary>
        /// Generate SrcSet attribute value based on a width and height for the image cropped around the focal point using a specific image cropper property alias, output format and optional quality
        /// </summary>
        /// <param name="publishedContent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="quality">Default is 90</param>
        /// <param name="outputFormat"></param>
        /// <param name="furtherOptions"></param>
        /// <returns>Url of image</returns>
        public HtmlString GetSrcSetUrls(IPublishedContent publishedContent, int width, int height, string propertyAlias = Constants.Conventions.Media.File, int quality = 90, string outputFormat = "", string furtherOptions = "")
        {
            var w = this.WidthStep();
            var q = quality == 90 ? this.DefaultQuality() : quality;

            var outputStringBuilder = new StringBuilder();
            var heightRatio = (decimal)height / width;

            while (w <= this.MaxWidth(publishedContent))
            {
                var h = (int)Math.Round(w * heightRatio);
                var cropString = this.GetCropUrl(publishedContent, w, h, propertyAlias, quality: q, preferFocalPoint: true,
                    furtherOptions: this.AdditionalParams(outputFormat, furtherOptions), htmlEncode: false).ToString();

                outputStringBuilder.Append($"{cropString} {w}w,");
                w += this.WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        #region Internal Functions

        private string MimeType(string fileExtension)
        {
            var defaultMimeType = "";
            switch (fileExtension)
            {
                case "jpg":
                    defaultMimeType = "image/jpeg";
                    break;
                case "png":
                    defaultMimeType = "image/png";
                    break;
                case "gif":
                    defaultMimeType = "image/gif";
                    break;
                case "webp":
                    defaultMimeType = "image/webp";
                    break;
                default:
                    defaultMimeType = "image/jpeg";
                    break;
            }

            return defaultMimeType;
        }

        private IPublishedContent GetAnyTypePublishedContent(GuidUdi guidUdi)
        {
            if (_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext context))
            {
                switch (guidUdi.EntityType)
                {
                    case Constants.UdiEntityType.Media:
                        return context.Media.GetById(guidUdi.Guid);

                    case Constants.UdiEntityType.Document:
                        return context.Content.GetById(guidUdi.Guid);

                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }
            
        }

        private int DefaultQuality()
        {
            return this._slimsyOptions.DefaultQuality;
        }

        private int WidthStep()
        {
            return this._slimsyOptions.WidthStep;
        }

        private int MaxWidth(IPublishedContent publishedContent)
        {
            var maxWidth = this._slimsyOptions.MaxWidth;

            // if publishedContent is a media item we can see if we can get the source image width & height
            if (publishedContent.ItemType == PublishedItemType.Media)
            {
                var sourceWidth = publishedContent.Value<int>(Constants.Conventions.Media.Width);

                // if source width is less than max width then we should stop at source width
                if (sourceWidth < maxWidth)
                {
                    maxWidth = sourceWidth;
                }

                // if the source image is less than the step then max width should be the first step
                if (maxWidth < this.WidthStep())
                {
                    maxWidth = this.WidthStep();
                }
            }

            return maxWidth;
        }

        private string AdditionalParams(string outputFormat = null, string furtherOptions = null)
        {
            if (outputFormat == null)
            {
                var slimsyFormat = this._slimsyOptions.Format;
                outputFormat = slimsyFormat ?? "auto";
            }

            var slimsyBgColor = this._slimsyOptions.BackgroundColor;
            var bgColor = slimsyBgColor != null && slimsyBgColor != "false" ? slimsyBgColor : string.Empty;

            var returnString = new StringBuilder();

            if (!string.IsNullOrEmpty(bgColor))
            {
                returnString.Append($"&bgcolor={bgColor}");
            }

            if (!string.IsNullOrEmpty(furtherOptions))
            {
                returnString.Append(furtherOptions);
            }

            if (!string.IsNullOrEmpty(outputFormat))
            {
                returnString.Append($"&format={outputFormat}");
            }

            return returnString.ToString();
        }

        private string DomainPrefix()
        {
            return this._slimsyOptions.DomainPrefix;
        }

        #endregion

        #region GetCropUrl proxies

        /// <summary>
        /// Gets the ImageProcessor Url of a media item by the crop alias (using default media item property alias of "umbracoFile"). This method will prepend the Slimsy DomainPrefix if set.
        /// </summary>
        /// <param name="mediaItem">
        /// The IPublishedContent item.
        /// </param>
        /// <param name="cropAlias">
        /// The crop alias e.g. thumbnail
        /// </param>
        /// <param name="htmlEncode">
        /// Whether to HTML encode this URL - default is true - w3c standards require HTML attributes to be HTML encoded but this can be
        /// set to false if using the result of this method for CSS.
        /// </param>
        /// <returns></returns>
        public HtmlString GetCropUrl(IPublishedContent mediaItem, string cropAlias,
            bool htmlEncode = true)
        {
            if (mediaItem == null) return EmptyHtmlString;

            var url = this.DomainPrefix() + mediaItem.GetCropUrl(cropAlias: cropAlias, useCropDimensions: true);
            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        }

        /// <summary>
        /// Gets the ImageProcessor Url by the crop alias using the specified property containing the image cropper Json data on the IPublishedContent item. This method will prepend the Slimsy DomainPrefix if set.
        /// </summary>
        /// <param name="mediaItem">
        /// The IPublishedContent item.
        /// </param>
        /// <param name="propertyAlias">
        /// The property alias of the property containing the Json data e.g. umbracoFile
        /// </param>
        /// <param name="cropAlias">
        /// The crop alias e.g. thumbnail
        /// </param>
        /// <param name="htmlEncode">
        /// Whether to HTML encode this URL - default is true - w3c standards require HTML attributes to be HTML encoded but this can be
        /// set to false if using the result of this method for CSS.
        /// </param>
        /// <returns>
        /// The ImageProcessor.Web Url.
        /// </returns>
        public HtmlString GetCropUrl(IPublishedContent mediaItem, string propertyAlias,
            string cropAlias, bool htmlEncode = true)
        {
            if (mediaItem == null) return EmptyHtmlString;

            var url = this.DomainPrefix() + mediaItem.GetCropUrl(propertyAlias: propertyAlias, cropAlias: cropAlias, useCropDimensions: true);
            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        }

        /// <summary>
        /// Gets the ImageProcessor Url from the image path. This method will prepend the Slimsy DomainPrefix if set.
        /// </summary>
        /// <param name="mediaItem">
        /// The IPublishedContent item.
        /// </param>
        /// <param name="width">
        /// The width of the output image.
        /// </param>
        /// <param name="height">
        /// The height of the output image.
        /// </param>
        /// <param name="propertyAlias">
        /// Property alias of the property containing the Json data.
        /// </param>
        /// <param name="cropAlias">
        /// The crop alias.
        /// </param>
        /// <param name="quality">
        /// Quality percentage of the output image.
        /// </param>
        /// <param name="imageCropMode">
        /// The image crop mode.
        /// </param>
        /// <param name="imageCropAnchor">
        /// The image crop anchor.
        /// </param>
        /// <param name="preferFocalPoint">
        /// Use focal point to generate an output image using the focal point instead of the predefined crop if there is one
        /// </param>
        /// <param name="useCropDimensions">
        /// Use crop dimensions to have the output image sized according to the predefined crop sizes, this will override the width and height parameters
        /// </param>
        /// <param name="cacheBuster">
        /// Add a serialized date of the last edit of the item to ensure client cache refresh when updated
        /// </param>
        /// <param name="furtherOptions">
        /// These are any query string parameters (formatted as query strings) that ImageProcessor supports. For example:
        /// <example>
        /// <![CDATA[
        /// furtherOptions: "&bgcolor=fff"
        /// ]]>
        /// </example>
        /// </param>
        /// <param name="ratioMode">
        /// Use a dimension as a ratio
        /// </param>
        /// <param name="upScale">
        /// If the image should be upscaled to requested dimensions
        /// </param>
        /// <param name="htmlEncode">
        /// Whether to HTML encode this URL - default is true - w3c standards require HTML attributes to be HTML encoded but this can be
        /// set to false if using the result of this method for CSS.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public HtmlString GetCropUrl(IPublishedContent mediaItem,
            int? width = null,
            int? height = null,
            string propertyAlias = Constants.Conventions.Media.File,
            string cropAlias = null,
            int? quality = null,
            ImageCropMode? imageCropMode = null,
            ImageCropAnchor? imageCropAnchor = null,
            bool preferFocalPoint = false,
            bool useCropDimensions = false,
            bool cacheBuster = true,
            string furtherOptions = null,
            ImageCropRatioMode? ratioMode = null,
            bool upScale = true,
            bool htmlEncode = true)
        {
            if (mediaItem == null) return EmptyHtmlString;
            //ImageCropRatioMode? ratioMode = null, bool upScale = true seem to be missing from v9
            var url = this.DomainPrefix() + mediaItem.GetCropUrl(width: width, height: height, propertyAlias: propertyAlias, cropAlias: cropAlias, quality: quality, imageCropMode: imageCropMode,
                imageCropAnchor: imageCropAnchor, preferFocalPoint: preferFocalPoint, useCropDimensions: useCropDimensions, cacheBuster: cacheBuster, furtherOptions: furtherOptions);
            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        }

        /// <summary>
        /// Gets the ImageProcessor Url from the image path. This method will prepend the Slimsy DomainPrefix if set.
        /// </summary>
        /// <param name="imageUrl">
        /// The image url.
        /// </param>
        /// <param name="width">
        /// The width of the output image.
        /// </param>
        /// <param name="height">
        /// The height of the output image.
        /// </param>
        /// <param name="imageCropperValue">
        /// The Json data from the Umbraco Core Image Cropper property editor
        /// </param>
        /// <param name="cropAlias">
        /// The crop alias.
        /// </param>
        /// <param name="quality">
        /// Quality percentage of the output image.
        /// </param>
        /// <param name="imageCropMode">
        /// The image crop mode.
        /// </param>
        /// <param name="imageCropAnchor">
        /// The image crop anchor.
        /// </param>
        /// <param name="preferFocalPoint">
        /// Use focal point to generate an output image using the focal point instead of the predefined crop if there is one
        /// </param>
        /// <param name="useCropDimensions">
        /// Use crop dimensions to have the output image sized according to the predefined crop sizes, this will override the width and height parameters
        /// </param>
        /// <param name="cacheBusterValue">
        /// Add a serialized date of the last edit of the item to ensure client cache refresh when updated
        /// </param>
        /// <param name="furtherOptions">
        /// These are any query string parameters (formatted as query strings) that ImageProcessor supports. For example:
        /// <example>
        /// <![CDATA[
        /// furtherOptions: "&bgcolor=fff"
        /// ]]>
        /// </example>
        /// </param>
        /// <param name="ratioMode">
        /// Use a dimension as a ratio
        /// </param>
        /// <param name="upScale">
        /// If the image should be upscaled to requested dimensions
        /// </param>
        /// <param name="htmlEncode">
        /// Whether to HTML encode this URL - default is true - w3c standards require HTML attributes to be HTML encoded but this can be
        /// set to false if using the result of this method for CSS.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        //public HtmlString GetCropUrl(
        //    string imageUrl,
        //    int? width = null,
        //    int? height = null,
        //    string imageCropperValue = null,
        //    string cropAlias = null,
        //    int? quality = null,
        //    ImageCropMode? imageCropMode = null,
        //    ImageCropAnchor? imageCropAnchor = null,
        //    bool preferFocalPoint = false,
        //    bool useCropDimensions = false,
        //    string cacheBusterValue = null,
        //    string furtherOptions = null,
        //    ImageCropRatioMode? ratioMode = null,
        //    bool upScale = true,
        //    bool htmlEncode = true)
        //{
        //    var url = this.DomainPrefix() + imageUrl.GetCropUrl(width, height, imageCropperValue, cropAlias, quality, imageCropMode,
        //        imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBusterValue, furtherOptions, ratioMode,
        //        upScale);
        //    return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        //}

        //public HtmlString GetCropUrl(
        //    ImageCropperValue imageCropperValue,
        //    int? width = null,
        //    int? height = null,
        //    string cropAlias = null,
        //    int? quality = null,
        //    ImageCropMode? imageCropMode = null,
        //    ImageCropAnchor? imageCropAnchor = null,
        //    bool preferFocalPoint = false,
        //    bool useCropDimensions = false,
        //    string cacheBusterValue = null,
        //    string furtherOptions = null,
        //    ImageCropRatioMode? ratioMode = null,
        //    bool upScale = true,
        //    bool htmlEncode = true)
        //{
        //    if (imageCropperValue == null) return EmptyHtmlString;

        //    var imageUrl = imageCropperValue.Src;
        //    var url = this.DomainPrefix() + imageUrl.GetCropUrl(imageCropperValue, width, height, cropAlias, quality, imageCropMode,
        //        imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBusterValue, furtherOptions, ratioMode,
        //        upScale);
        //    return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        //}

        #endregion

        public enum ImageCropRatioMode
        {
            Width,
            Height
        }
    }
}
