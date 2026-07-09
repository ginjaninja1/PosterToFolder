
using MediaBrowser.Common;
using MediaBrowser.Model.Logging;
using PosterToFolder.UI.Config;
using PosterToFolder.UIBaseClasses.Store;

namespace PosterToFolder.Storage
{
    public class BasicsOptionsStore : SimpleFileStore<ConfigUI>
    {
        public BasicsOptionsStore(IApplicationHost applicationHost, ILogger logger, string pluginFullName)
        : base(applicationHost, logger, pluginFullName)
        {
        }
    }
}
