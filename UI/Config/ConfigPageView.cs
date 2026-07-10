using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Serialization;
using PosterToFolder.Storage;
using PosterToFolder.UIBaseClasses.Views;
using PosterToFolder.UI.Config;

namespace PosterToFolder.UI
{
    /// <summary>
    /// Config page. Deliberately kept to just: construction, page settings,
    /// and command handlers (including OnSaveCommand) that read/write the
    /// persisted configuration.
    ///
    /// Everything else has been split out:
    ///   - LibraryFilterCommands  : command id constants + build/parse
    ///   - LibraryPathReconciler  : domain logic for reconciling paths
    ///   - ConfigViewBuilder      : builds the on-screen GenericItemList
    /// </summary>
    internal class ConfigPageView : PluginPageView
    {
        private readonly BasicsOptionsStore store;
        private readonly ILibraryManager libraryManager;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ILogger logger;

        public ConfigPageView(
            PluginInfo pluginInfo,
            IServerApplicationHost applicationHost,
            BasicsOptionsStore store,
            ILogger logger)
            : base(pluginInfo.Id)
        {
            this.store = store;
            this.logger = logger;
            this.libraryManager = applicationHost.Resolve<ILibraryManager>();
            this.jsonSerializer = applicationHost.Resolve<IJsonSerializer>();

            RebuildContentData();
        }

        /// <summary>
        /// Reloads the persisted config, reconciles it against Emby's current
        /// library layout, and rebuilds ContentData from it.
        ///
        /// NOTE: ContentData is always a freshly-built display object (see
        /// ConfigViewBuilder), never the persisted instance itself. This is
        /// what stops the visual LibraryList from ever being written to the
        /// JSON config file.
        /// </summary>
        private void RebuildContentData()
        {
            var config = this.store.GetOptions();
            var currentFolders = this.libraryManager.GetVirtualFolders();

            LibraryPathReconciler.EnsureDiscoveredPaths(config, currentFolders);

            this.ContentData = ConfigViewBuilder.BuildDisplayConfig(config, currentFolders);
        }

        public override Task<IPluginUIView> OnSaveCommand(
            string itemId,
            string commandId,
            string data)
        {
            return RunCommand(itemId, commandId, data);
        }

        public override Task<IPluginUIView> RunCommand(
            string itemId,
            string commandId,
            string data)
        {
            if (!string.IsNullOrEmpty(data) &&
                commandId == LibraryFilterCommands.PageSave)
            {
                HandleSave(data);
                return Task.FromResult<IPluginUIView>(this);
            }

            if (LibraryFilterCommands.TryParseLibraryToggle(commandId, out var libraryName))
            {
                HandleLibraryToggle(libraryName);
                return Task.FromResult<IPluginUIView>(this);
            }

            if (LibraryFilterCommands.TryParsePathToggle(commandId, out var pathLibraryName, out var path))
            {
                HandlePathToggle(pathLibraryName, path);
                return Task.FromResult<IPluginUIView>(this);
            }

            return Task.FromResult<IPluginUIView>(this);
        }

        private void HandleSave(string data)
        {
            var config = this.store.GetOptions();

            try
            {
                var incoming = this.jsonSerializer.DeserializeFromString<ConfigUI>(data);

                if (incoming != null)
                {
                    config.EnablePlugin = incoming.EnablePlugin;
                    config.LibraryPaths = incoming.LibraryPaths ?? new List<LibraryPathFilterItem>();

                    this.store.SetOptions(config);

                    this.logger.Info("Poster To Folder configuration saved");
                }
            }
            catch (Exception ex)
            {
                this.logger.ErrorException("Error saving Poster To Folder configuration", ex);
            }

            RebuildContentData();
        }

        private void HandleLibraryToggle(string libraryName)
        {
            var config = this.store.GetOptions();
            var currentFolders = this.libraryManager.GetVirtualFolders();

            if (LibraryPathReconciler.ToggleLibrary(config, currentFolders, libraryName))
            {
                this.store.SetOptions(config);
            }

            RebuildContentData();
            RaiseUIViewInfoChanged();
        }

        private void HandlePathToggle(string libraryName, string path)
        {
            var config = this.store.GetOptions();
            var currentFolders = this.libraryManager.GetVirtualFolders();

            if (LibraryPathReconciler.TogglePath(config, currentFolders, libraryName, path))
            {
                this.store.SetOptions(config);
            }

            RebuildContentData();
            RaiseUIViewInfoChanged();
        }
    }
}