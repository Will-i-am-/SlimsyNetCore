namespace Slimsy.Services
{
    using System.Collections.Specialized;
    using System.Text;
    using System.Web;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Html;
    using Newtonsoft.Json;
    using Slimsy.Enums;
    using Slimsy.Interfaces;
    using Slimsy.Models;
    using Umbraco.Cms.Core;
    using Umbraco.Cms.Core.Strings;
    using Umbraco.Cms.Core.Models;
    using Umbraco.Cms.Core.Models.PublishedContent;
    using Umbraco.Cms.Core.PropertyEditors;
    using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
    using Umbraco.Cms.Core.Web;
    using Umbraco.Extensions;

    public class SlimsyService
    {
        private readonly ISlimsyOptions _slimsyOptions;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private static readonly HtmlString EmptyHtmlString = new HtmlString(string.Empty);
        private readonly RteMacroRenderingValueConverter _rteMacroRenderingValueConverter;

        public SlimsyService(ISlimsyOptions slimsyOptions, IUmbracoContextAccessor umbracoContextAccessor, RteMacroRenderingValueConverter rteMacroRenderingValueConverter)
        {
            this._slimsyOptions = slimsyOptions;
            this._umbracoContextAccessor = umbracoContextAccessor;
            this._rteMacroRenderingValueConverter = rteMacroRenderingValueConverter;
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

        /// <summary>
        /// Generate SrcSet attribute value based on a width and height for the image cropped using a specific mode and using a specific image cropper property alias, output format and optional quality
        /// </summary>
        /// <param name="publishedContent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="imageCropMode"></param>
        /// <param name="imageCropAnchor"></param>
        /// <param name="quality">Default is 90</param>
        /// <param name="outputFormat"></param>
        /// <param name="furtherOptions"></param>
        /// <returns>Url of image</returns>
        public HtmlString GetSrcSetUrls(IPublishedContent publishedContent, int width, int height, ImageCropMode imageCropMode, ImageCropAnchor imageCropAnchor = ImageCropAnchor.Center, string propertyAlias = Constants.Conventions.Media.File, int quality = 90, string outputFormat = "", string furtherOptions = "")
        {
            var w = this.WidthStep();
            var q = quality == 90 ? this.DefaultQuality() : quality;

            var outputStringBuilder = new StringBuilder();
            var heightRatio = (decimal)height / width;

            while (w <= this.MaxWidth(publishedContent))
            {
                var h = (int)Math.Round(w * heightRatio);
                var cropString = this.GetCropUrl(publishedContent, w, h, propertyAlias, quality: q, furtherOptions: this.AdditionalParams(outputFormat, furtherOptions), imageCropMode: imageCropMode, imageCropAnchor: imageCropAnchor, htmlEncode: false).ToString();

                outputStringBuilder.Append($"{cropString} {w}w,");
                w += this.WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));

        }

        /// <summary>
        /// Generate SrcSet attribute value  based on a width and height for the image passing in a ratio for the image
        /// </summary>
        /// <param name="publishedContent"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="quality">Default is 90</param>
        /// <param name="outputFormat"></param>
        /// <param name="furtherOptions"></param>
        /// <returns>HTML Markup</returns>
        public HtmlString GetSrcSetUrls(IPublishedContent publishedContent, AspectRatio aspectRatio, string propertyAlias = Constants.Conventions.Media.File, int quality = 90, string outputFormat = "", string furtherOptions = "")
        {
            var w = this.WidthStep();
            var q = quality == 90 ? this.DefaultQuality() : quality;

            var outputStringBuilder = new StringBuilder();

            while (w <= this.MaxWidth(publishedContent))
            {
                var heightRatio = (decimal)aspectRatio.Height / aspectRatio.Width;

                var h = (int)Math.Round(w * heightRatio);

                outputStringBuilder.Append(
                    $"{this.GetCropUrl(publishedContent, w, h, propertyAlias, quality: q, preferFocalPoint: true, furtherOptions: AdditionalParams(outputFormat, furtherOptions), htmlEncode: false)} {w}w,");

                w += this.WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        /// <summary>
        /// Generate SrcSet attribute value based on a width and height for a static image
        /// </summary>
        /// <param name="url">The url of a image</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="maxWidth">The maximum width to generate urls for, this should be the actual width of the source image</param>
        /// <param name="quality">Default is 90</param>
        /// <param name="imageCropMode"></param>
        /// <param name="imageCropAnchor"></param>
        /// <param name="outputFormat"></param>
        /// <returns>Url of image</returns>
        public HtmlString GetSrcSetUrls(string url, int width, int height, int maxWidth, int quality = 90, ImageCropMode? imageCropMode = null, ImageCropAnchor? imageCropAnchor = null, string outputFormat = "")
        {
            var w = this.WidthStep();
            var q = quality == 90 ? this.DefaultQuality() : quality;

            var outputStringBuilder = new StringBuilder();
            var heightRatio = (decimal)height / width;

            while (w <= maxWidth)
            {
                var h = (int)Math.Round(w * heightRatio);
                outputStringBuilder.Append(
                    $"{this.GetCropUrl(url, w, h, imageCropMode: imageCropMode, imageCropAnchor: imageCropAnchor, quality: q, preferFocalPoint: true, furtherOptions: AdditionalParams(outputFormat), htmlEncode: false)} {w}w,");
                w += this.WidthStep();
            }

            // remove the last comma
            var outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        #region Pre defined crops

        /// <summary>
        /// Generate SrcSet attribute value based on a predefined crop using a specific image cropper property alias, output format and optional quality
        /// </summary>
        /// <param name="publishedContent"></param>
        /// <param name="cropAlias"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="outputFormat"></param>
        /// <param name="furtherOptions"></param>
        /// <param name="quality">Default is 90</param>
        /// <returns>Url of image</returns>
        public HtmlString GetSrcSetUrls(IPublishedContent publishedContent, string cropAlias, string propertyAlias = Constants.Conventions.Media.File, int quality = 90, string outputFormat = "", string furtherOptions = "")
        {
            var w = this.WidthStep();
            var q = quality == 90 ? this.DefaultQuality() : quality;

            var outputStringBuilder = new StringBuilder();
            var outputString = string.Empty;

            var cropperJson = publishedContent.Value<string>(propertyAlias);
            var imageCrops = JsonConvert.DeserializeObject<ImageCropperValue>(cropperJson);
            var crop = imageCrops?.Crops?.FirstOrDefault(x => x.Alias.InvariantEquals(cropAlias));
            var additionalParams = this.AdditionalParams(outputFormat, furtherOptions);

            if (crop != null)
            {
                var heightRatio = (decimal)crop.Height / crop.Width;
                while (w <= this.MaxWidth(publishedContent))
                {
                    var h = (int)Math.Round(w * heightRatio);
                    outputStringBuilder.Append(
                        $"{this.GetCropUrl(publishedContent, w, h, propertyAlias, cropAlias, q, furtherOptions: additionalParams, htmlEncode: false)} {w}w,");
                    w += this.WidthStep();
                }

                // remove the last comma
                outputString = outputStringBuilder.ToString().Substring(0, outputStringBuilder.Length - 1);
            }
            else
            {
                // this code would execute if a predefined crop has been added to the data type but this media item hasn't been re-saved
                var cropperConfiguration = (ImageCropperConfiguration)publishedContent.Properties.FirstOrDefault(x => x.Alias == propertyAlias)?.PropertyType.DataType.Configuration;
                var cropConfiguration = cropperConfiguration?.Crops.FirstOrDefault(c => c.Alias == cropAlias);
                if (cropConfiguration != null)
                {
                    // auto generate using focal point
                    return this.GetSrcSetUrls(publishedContent, cropConfiguration.Width,
                        cropConfiguration.Height, propertyAlias, outputFormat: outputFormat, quality: q, furtherOptions: additionalParams);
                }
            }

            return new HtmlString(HttpUtility.HtmlEncode(outputString));
        }

        #endregion

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

        private HtmlString ConvertImgToResponsiveInternal(string html, bool generateLqip = true,
            bool removeStyleAttribute = false, bool removeUdiAttribute = true, bool roundWidthHeight = true,
            bool renderPicture = false, string[] pictureSources = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // https://stackoverflow.com/questions/759355/image-tag-not-closing-with-htmlagilitypack
            if (HtmlNode.ElementsFlags.ContainsKey("img"))
            {
                HtmlNode.ElementsFlags["img"] = HtmlElementFlag.Closed;
            }
            else
            {
                HtmlNode.ElementsFlags.Add("img", HtmlElementFlag.Closed);
            }
            if (HtmlNode.ElementsFlags.ContainsKey("source"))
            {
                HtmlNode.ElementsFlags["source"] = HtmlElementFlag.Closed;
            }
            else
            {
                HtmlNode.ElementsFlags.Add("source", HtmlElementFlag.Closed);
            }

            if (!doc.ParseErrors.Any() && doc.DocumentNode != null)
            {
                // Find all images
                var imgNodes = doc.DocumentNode.SelectNodes("//img");

                if (imgNodes != null)
                {
                    var modified = false;

                    foreach (var imgElement in imgNodes)
                    {
                        var img = imgElement;

                        if (renderPicture)
                        {
                            var newImg = img.CloneNode(true);

                            // change img to picture and add new img as child
                            imgElement.Name = "picture";
                            imgElement.RemoveAll();
                            imgElement.ChildNodes.Add(newImg);
                            img = imgElement.FirstChild;
                        }

                        var srcAttr = img.Attributes.FirstOrDefault(x => x.Name == "src");
                        var udiAttr = img.Attributes.FirstOrDefault(x => x.Name == "data-udi");
                        var classAttr = img.Attributes.FirstOrDefault(x => x.Name == "class");

                        if (srcAttr != null)
                        {
                            // html decode the url as variables encoded in tinymce
                            var src = HttpUtility.HtmlDecode(srcAttr.Value);

                            var hasQueryString = src.InvariantContains("?");
                            NameValueCollection queryStringCollection;

                            if (hasQueryString)
                            {
                                queryStringCollection = HttpUtility.ParseQueryString(src.Substring(src.IndexOf('?')));

                                // ensure case of variables doesn't cause trouble
                                IDictionary<string, string> queryString = queryStringCollection.AllKeys.ToDictionary(k => k.ToLowerInvariant(), k => queryStringCollection[k]);

                                if (udiAttr != null)
                                {
                                    // Umbraco media
                                    GuidUdi guidUdi;
                                    if (UdiParser.TryParse(udiAttr.Value, out guidUdi))
                                    {
                                        var node = this.GetAnyTypePublishedContent(guidUdi);

                                        var qsWidth = queryString["width"];
                                        var qsHeight = "0";
                                        if (queryString.ContainsKey("height"))
                                        {
                                            qsHeight = queryString["height"];
                                        }

                                        // TinyMce sometimes adds decimals to image resize commands, we need to fix those
                                        if (decimal.TryParse(qsWidth, out decimal decWidth) && decimal.TryParse(qsHeight, out decimal decHeight))
                                        {
                                            var width = (int)Math.Round(decWidth);
                                            var height = (int)Math.Round(decHeight);

                                            // if width is 0 (I don't know why it would be but it has been seen) then we can't do anything
                                            if (width > 0)
                                            {
                                                // change the src attribute to data-src
                                                srcAttr.Name = "data-src";
                                                if (roundWidthHeight)
                                                {
                                                    var roundedUrl = this.GetCropUrl(node, width, height,
                                                        imageCropMode: ImageCropMode.Pad, preferFocalPoint: true);
                                                    srcAttr.Value = roundedUrl.ToString();
                                                }

                                                var srcSet = this.GetSrcSetUrls(node, width, height);

                                                HtmlString defaultLqip = null;
                                                if (generateLqip)
                                                {
                                                    defaultLqip = this.GetCropUrl(node, width, height, quality: 30,
                                                        furtherOptions: "&format=auto", preferFocalPoint: true);
                                                }

                                                if (renderPicture)
                                                {
                                                    var umbracoExtension = node.Value<string>(Constants.Conventions.Media.Extension);

                                                    if (pictureSources == null || !pictureSources.Contains(umbracoExtension))
                                                    {
                                                        var defaultSource = HtmlNode.CreateNode($"<source data-srcset=\"{srcSet.ToString()}\" type=\"{MimeType(umbracoExtension)}\" data-sizes=\"auto\" />");
                                                        if (generateLqip)
                                                        {
                                                            defaultSource.Attributes.Add("srcset", defaultLqip.ToString());
                                                        }

                                                        imgElement.ChildNodes.Insert(0, defaultSource);
                                                    }

                                                    if (pictureSources != null)
                                                    {
                                                        foreach (var source in pictureSources.Reverse())
                                                        {
                                                            var srcSetForSource = this.GetSrcSetUrls(node, width,
                                                                height, outputFormat: source);
                                                            var sourceElement =
                                                                HtmlNode.CreateNode(
                                                                    $"<source data-srcset=\"{srcSetForSource.ToString()}\" type=\"{MimeType(source)}\" data-sizes=\"auto\" />");

                                                            if (generateLqip)
                                                            {
                                                                var sourceLqip = this.GetCropUrl(node, width, height, quality: 30,
                                                                    furtherOptions: $"&format={source}", preferFocalPoint: true);
                                                                sourceElement.Attributes.Add("srcset", sourceLqip.ToString());
                                                            }

                                                            imgElement.ChildNodes.Insert(0, sourceElement);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    img.Attributes.Add("data-srcset", srcSet.ToString());
                                                }

                                                img.Attributes.Add("data-sizes", "auto");

                                                if (generateLqip)
                                                {
                                                    img.Attributes.Add("src", defaultLqip.ToString());
                                                }

                                                if (classAttr != null)
                                                {
                                                    classAttr.Value = $"{classAttr.Value} lazyload";
                                                }
                                                else
                                                {
                                                    img.Attributes.Add("class", "lazyload");
                                                }

                                                if (removeStyleAttribute)
                                                {
                                                    img.Attributes.Remove("style");
                                                }

                                                if (removeUdiAttribute)
                                                {
                                                    img.Attributes.Remove("data-udi");
                                                }

                                                modified = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Image in TinyMce doesn't have a data-udi attribute
                            }
                        }
                    }

                    if (modified)
                    {
                        return new HtmlString(doc.DocumentNode.OuterHtml);
                    }
                }
            }

            return new HtmlString(html);
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
        public HtmlString GetCropUrl(
            string imageUrl,
            int? width = null,
            int? height = null,
            string imageCropperValue = null,
            string cropAlias = null,
            int? quality = null,
            ImageCropMode? imageCropMode = null,
            ImageCropAnchor? imageCropAnchor = null,
            bool preferFocalPoint = false,
            bool useCropDimensions = false,
            string cacheBusterValue = null,
            string furtherOptions = null,
            ImageCropRatioMode? ratioMode = null,
            bool upScale = true,
            bool htmlEncode = true)
        {
            // ratioMode, upScale - MISSING FROM v9 EXTENSIONS
            var url = this.DomainPrefix() + imageUrl.GetCropUrl(width, height, imageCropperValue, cropAlias, quality, imageCropMode, imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBusterValue, furtherOptions);
            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        }

        public HtmlString GetCropUrl(
            ImageCropperValue imageCropperValue,
            int? width = null,
            int? height = null,
            string cropAlias = null,
            int? quality = null,
            ImageCropMode? imageCropMode = null,
            ImageCropAnchor? imageCropAnchor = null,
            bool preferFocalPoint = false,
            bool useCropDimensions = false,
            string cacheBusterValue = null,
            string furtherOptions = null,
            ImageCropRatioMode? ratioMode = null,
            bool upScale = true,
            bool htmlEncode = true)
        {
            if (imageCropperValue == null) return EmptyHtmlString;

            var imageUrl = imageCropperValue.Src;
            var url = this.DomainPrefix() + imageUrl.GetCropUrl(imageCropperValue, width, height, cropAlias, quality, imageCropMode,
                imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBusterValue, furtherOptions);
            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(url)) : new HtmlString(url);
        }

        #endregion

        #region HTML Helpers

        /// <summary>
        /// Convert img to img srcset, extracts width and height from querystrings
        /// </summary>
        /// <param name="sourceValueHtml">This html value should be the source value from and Umbraco property or a raw grid RTE value</param>
        /// <param name="generateLqip"></param>
        /// <param name="removeStyleAttribute">If you don't want the inline style attribute added by TinyMce to render</param>
        /// <param name="renderPicture"></param>
        /// <param name="pictureSources"></param>
        /// <returns>HTML Markup</returns>
        public IHtmlEncodedString ConvertImgToResponsive(string sourceValueHtml, bool generateLqip = true, bool removeStyleAttribute = true, bool renderPicture = false, string[] pictureSources = null)
        {
            var source = this.ConvertImgToResponsiveInternal(sourceValueHtml, generateLqip, removeStyleAttribute, renderPicture: renderPicture, pictureSources: pictureSources);

            // We have the raw value so we need to run it through the value converter to ensure that links and macros are rendered
            var intermediateValue = this._rteMacroRenderingValueConverter.ConvertSourceToIntermediate(null, null, source, false);
            var objectValue = this._rteMacroRenderingValueConverter.ConvertIntermediateToObject(null, null, 0, intermediateValue, false);
            return objectValue as IHtmlEncodedString;
        }

        /// <summary>
        /// Convert img to img srcset, extracts width and height from querystrings
        /// </summary>
        /// <param name="publishedContent"></param>
        /// <param name="propertyAlias">Alias of the TinyMce property</param>
        /// <param name="generateLqip">Set to true if you want LQIP markup to be generated</param>
        /// <param name="removeStyleAttribute">If you don't want the inline style attribute added by TinyMce to render</param>
        /// <param name="renderPicture"></param>
        /// <param name="pictureSources"></param>
        /// <returns>HTML Markup</returns>
        public IHtmlEncodedString ConvertImgToResponsive(IPublishedContent publishedContent, string propertyAlias, bool generateLqip = true, bool removeStyleAttribute = true, bool renderPicture = false, string[] pictureSources = null)
        {
            var sourceValue = publishedContent.GetProperty(propertyAlias).GetSourceValue();

            return sourceValue != null ?
                this.ConvertImgToResponsive(sourceValue.ToString(), generateLqip, removeStyleAttribute, renderPicture, pictureSources) :
                new HtmlEncodedString("");
        }


        #endregion

    }
}
