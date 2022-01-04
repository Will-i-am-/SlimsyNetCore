namespace Slimsy.Services
{
    public class SlimsyService
    {
        private readonly ILogger _logger;
        private readonly ISlimsyOptions _slimsyOptions;

        public SlimsyService(ISlimsyOptions slimsyOptions, ILogger logger)
        {
            this._slimsyOptions = slimsyOptions;
            this._logger = logger;
        }

        public int GetWidthStep()
        {
            return _slimsyOptions.WidthStep;
        }

    }
}
