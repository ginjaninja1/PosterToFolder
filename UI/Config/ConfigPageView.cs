using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using PosterToFolder.Storage;
using PosterToFolder.UIBaseClasses.Views;
using System.Threading.Tasks;

namespace PosterToFolder.UI.Config
{
    internal class ConfigPageView : PluginPageView
    {
        private readonly BasicsOptionsStore store;
        private readonly ILogger logger;

        public ConfigPageView(
            PluginInfo pluginInfo,
            BasicsOptionsStore store,
            ILogger logger)
            : base(pluginInfo.Id)
        {
            this.store = store;
            this.logger = logger;
            this.ContentData = store.GetOptions();
        }

        public ConfigUI ConfigUI => this.ContentData as ConfigUI;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
        {
            this.logger.Info(
    "Poster To Folder SaveCommand fired. ItemId: {0}, CommandId: {1}, Data: {2}",
    itemId,
    commandId,
    data);

            if (this.ConfigUI == null)
            {
                this.logger.Error("Poster To Folder: ConfigUI is null during save");
            }
            else
            {
                this.logger.Info("Poster To Folder: Saving configuration");
            }

            this.store.SetOptions(this.ConfigUI);

            this.logger.Info("Poster To Folder: Configuration saved");

            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}