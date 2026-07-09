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

        public ConfigPageView(PluginInfo pluginInfo, BasicsOptionsStore store)
        : base(pluginInfo.Id)
        {
            this.store = store;
            this.ContentData = store.GetOptions();
        }

        public ConfigUI BasicsUI => this.ContentData as ConfigUI;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
        {
            this.store.SetOptions(this.BasicsUI);
            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}