namespace Slimsy.Configuration
{
    using Microsoft.Extensions.Options;

    public class SlimsyWebConfigOptions : ISlimsyOptions
    {
        private readonly IOptions<SlimsyConfig> _config;
        public SlimsyWebConfigOptions(IOptions<SlimsyConfig> config)
        {
            this._config = config;

            var slimsyDefaultQuality = this._config.Value.DefaultQuality;
            if (slimsyDefaultQuality == 0)
            {
                slimsyDefaultQuality = 90;
            }

            var slimsyWidthStep = this._config.Value.WidthStep;
            if (slimsyWidthStep == 0)
            {
                slimsyWidthStep = 160;
            }

            var slimsyMaxWidth = this._config.Value.MaxWidth;
            if (slimsyMaxWidth == 0)
            {
                slimsyMaxWidth = 2048;
            }

            var slimsyFormat = this._config.Value.Format;
            var outputFormat = slimsyFormat ?? "auto";

            var slimsyBgColor = this._config.Value.BackgroundColor;
            var bgColor = slimsyBgColor != "false" ? slimsyBgColor : string.Empty;

            var domainPrefix = this._config.Value.DomainPrefix;
            if (string.IsNullOrEmpty(domainPrefix))
                domainPrefix = string.Empty;

            Format = outputFormat;
            BackgroundColor = bgColor;
            DefaultQuality = slimsyDefaultQuality;
            MaxWidth = slimsyMaxWidth;
            WidthStep = slimsyWidthStep;
            DomainPrefix = domainPrefix;
        }

        public string Format { get; set; }
        public string BackgroundColor { get; set; }
        public int DefaultQuality { get; set; }
        public int MaxWidth { get; set; }
        public int WidthStep { get; set; }
        public string DomainPrefix { get; set; }
    }
}
