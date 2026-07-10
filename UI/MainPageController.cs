using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Plugins.UI.Views;
using PosterToFolder.Storage;
using PosterToFolder.UI.Config;
// consider adding using statements for each of the UI classes
using PosterToFolder.UIBaseClasses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosterToFolder.UI
{
    internal class MainPageController : ControllerBase, IHasTabbedUIPages
    {
        private readonly PluginInfo pluginInfo;
        private readonly BasicsOptionsStore basicsOptionsStore;
        private readonly List<IPluginUIPageController> tabPages = new List<IPluginUIPageController>();
        private readonly ILogger logger;

        /// <summary>Initializes a new instance of the <see cref="ControllerBase" /> class.</summary>
        /// <param name="pluginInfo">The plugin information.</param>
        /// <param name="applicationHost"></param>
        /// <param name="basicsOptionsStore"></param>
        public MainPageController(PluginInfo pluginInfo, IServerApplicationHost applicationHost, BasicsOptionsStore basicsOptionsStore,
    ILogger logger)
            : base(pluginInfo.Id)
        {
            this.pluginInfo = pluginInfo;
            this.basicsOptionsStore = basicsOptionsStore;
            this.logger = logger;
            this.PageInfo = new PluginPageInfo
            {
                Name = "PosterToFolder",
                EnableInMainMenu = false,
                DisplayName = "PosterToFolder",
                MenuIcon = "list_alt",
                IsMainConfigPage = true,
            };

            //Add adition PluginUIViews
            //this.tabPages.Add(new TabPageController(pluginInfo, nameof(SelectionPageView), "Selection", e => new SelectionPageView(pluginInfo)));

        }


        public override PluginPageInfo PageInfo { get; }

        public override Task<IPluginUIView> CreateDefaultPageView()
        {
            IPluginUIView view = new ConfigPageView(
    this.pluginInfo,
    this.basicsOptionsStore,
    this.logger);
            return Task.FromResult(view);
        }

        public IReadOnlyList<IPluginUIPageController> TabPageControllers => this.tabPages.AsReadOnly();
    }
}
