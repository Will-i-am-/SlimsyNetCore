namespace Slimsy
{
    using global::Slimsy.Configuration;
    using global::Slimsy.Services;
    using Microsoft.Extensions.Options;
    using Umbraco.Cms.Core.Composing;
    using Umbraco.Cms.Core.DependencyInjection;
    using Umbraco.Extensions;

    public class SlimsyComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddUnique<ISlimsyOptions, SlimsyWebConfigOptions>();
            builder.Services.AddSingleton<SlimsyService>();
        }
    }
}
