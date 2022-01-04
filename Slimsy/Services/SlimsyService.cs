namespace Slimsy.Services
{
    public class SlimsyService
    {
        private readonly ISlimsyOptions _slimsyOptions;

        public SlimsyService(ISlimsyOptions slimsyOptions)
        {
            this._slimsyOptions = slimsyOptions;
        }

        public int GetWidthStep()
        {
            return _slimsyOptions.WidthStep;
        }

    }
}
