using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Serialization;
using PosterToFolder.Storage;
using PosterToFolder.UIBaseClasses.Views;
using PosterToFolder.UI.Config;

namespace PosterToFolder.UI
{
    internal class ConfigPageView : PluginPageView
    {
        private const string LibraryTogglePrefix = "togglelib:";
        private const string PathTogglePrefix = "togglepath:";
        private const string PathSeparator = "|||";

        private readonly BasicsOptionsStore store;
        private readonly ILibraryManager libraryManager;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ILogger logger;

        private ConfigUI VM => this.ContentData as ConfigUI;


        public ConfigPageView(
            PluginInfo pluginInfo,
            IServerApplicationHost applicationHost,
            BasicsOptionsStore store,
            ILogger logger)
            : base(pluginInfo.Id)
        {
            this.store = store;
            this.libraryManager = applicationHost.Resolve<ILibraryManager>();
            this.jsonSerializer = applicationHost.Resolve<IJsonSerializer>();
            

            var config = this.store.GetOptions();

            EnsureLibraryPaths(config);

            BuildLibraryList(config);

            this.ContentData = config;
        }

        private static bool IsRelevantLibrary(VirtualFolderInfo folder)
        {
            return string.IsNullOrEmpty(folder.CollectionType)
                || string.Equals(folder.CollectionType, "movies", StringComparison.OrdinalIgnoreCase)
                || string.Equals(folder.CollectionType, "tvshows", StringComparison.OrdinalIgnoreCase);
        }

        private List<VirtualFolderInfo> GetRelevantLibraries()
        {
            return this.libraryManager
                .GetVirtualFolders()
                .Where(IsRelevantLibrary)
                .OrderBy(f => f.Name)
                .ToList();
        }


        private void EnsureLibraryPaths(ConfigUI config)
        {
            if (config.LibraryPaths == null)
            {
                config.LibraryPaths = new List<LibraryPathFilterItem>();
            }

            var folders = this.libraryManager.GetVirtualFolders();

            foreach (var folder in folders)
            {
                foreach (var location in folder.Locations)
                {
                    var existing = config.LibraryPaths.FirstOrDefault(x =>
                        string.Equals(
                            x.Path,
                            location,
                            StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        config.LibraryPaths.Add(new LibraryPathFilterItem
                        {
                            LibraryName = folder.Name,
                            Path = location,
                            Enabled = true
                        });
                    }
                }
            }
        }

        private void EnsureFiltersUpToDate(
    ConfigUI config,
    List<VirtualFolderInfo> folders)
        {
            if (config.LibraryPaths == null)
            {
                config.LibraryPaths = new List<LibraryPathFilterItem>();
            }

            foreach (var folder in folders)
            {
                foreach (var location in folder.Locations)
                {
                    var existing = config.LibraryPaths.FirstOrDefault(x =>
                        string.Equals(x.LibraryName, folder.Name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.Path, location, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        config.LibraryPaths.Add(new LibraryPathFilterItem
                        {
                            LibraryName = folder.Name,
                            Path = location,
                            Enabled = true
                        });
                    }
                }
            }
        }


        private void BuildLibraryList(ConfigUI config)
        {
            config.LibraryList.Clear();

            var folders = GetRelevantLibraries();


            foreach (var folder in folders)
            {
                var paths = config.LibraryPaths
                    .Where(x => string.Equals(
                        x.LibraryName,
                        folder.Name,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();


                var subItems = new GenericItemList();

                int enabledPaths = 0;

                foreach (var path in paths)
                {
                    if (path.Enabled)
                    {
                        enabledPaths++;
                    }

                    subItems.Add(new GenericListItem
                    {
                        PrimaryText = path.Path,
                        Icon = IconNames.folder,
                        IconMode = ItemListIconMode.SmallRegular,

                        Status = path.Enabled
                            ? ItemStatus.Succeeded
                            : ItemStatus.Unavailable,

                        Toggle = new ToggleButtonItem("In Scope")
                        {
                            IsChecked = path.Enabled,
                            CommandId =
                                $"{PathTogglePrefix}{folder.Name}{PathSeparator}{path.Path}"
                        }
                    });
                }


                bool libraryEnabled = paths.Any(x => x.Enabled);

                string description;

                if (!libraryEnabled)
                {
                    description =
                        "Disabled - Enable library and 1 or more paths to include";
                }
                else
                {
                    description =
                        $"{enabledPaths} of {paths.Count} paths monitored";
                }


                config.LibraryList.Add(new GenericListItem
                {
                    PrimaryText = folder.Name,
                    SecondaryText = description,

                    Icon = IconNames.video_library,
                    IconMode = ItemListIconMode.LargeRegular,

                    Status = libraryEnabled
                        ? ItemStatus.Succeeded
                        : ItemStatus.Unavailable,

                    Toggle = new ToggleButtonItem("In Scope")
                    {
                        IsChecked = libraryEnabled,
                        CommandId =
                            $"{LibraryTogglePrefix}{folder.Name}"
                    },

                    SubItems = subItems
                });
            }
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
            var config = this.store.GetOptions();
            var ui = this.VM;

            if (ui == null)
            {
                return Task.FromResult<IPluginUIView>(this);
            }


            //
            // GenericUI sends the complete edited object back as JSON.
            //
            if (!string.IsNullOrEmpty(data) &&
                commandId == "PageSave")
            {
                try
                {
                    var incoming =
                        this.jsonSerializer
                            .DeserializeFromString<ConfigUI>(data);

                    if (incoming != null)
                    {
                        config.EnablePlugin = incoming.EnablePlugin;
                        config.LibraryPaths = incoming.LibraryPaths ??
                                              new List<LibraryPathFilterItem>();

                        this.store.SetOptions(config);

                        this.logger.Info(
                            "Poster To Folder configuration saved");
                    }
                }
                catch (Exception ex)
                {
                    this.logger.ErrorException(
                        "Error saving Poster To Folder configuration",
                        ex);
                }


                return Task.FromResult<IPluginUIView>(this);
            }



            //
            // Library toggle
            //
            if (commandId.StartsWith(
                    LibraryTogglePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                var libraryName =
                    commandId.Substring(LibraryTogglePrefix.Length);


                var libraryPaths = config.LibraryPaths
                    .Where(x =>
                        string.Equals(
                            x.LibraryName,
                            libraryName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();


                if (libraryPaths.Count > 0)
                {
                    bool newState =
                        !libraryPaths.Any(x => x.Enabled);


                    foreach (var path in libraryPaths)
                    {
                        path.Enabled = newState;
                    }


                    BuildLibraryList(config);

                    this.store.SetOptions(config);

                    RaiseUIViewInfoChanged();
                }


                return Task.FromResult<IPluginUIView>(this);
            }



            //
            // Path toggle
            //
            if (commandId.StartsWith(
                    PathTogglePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                var payload =
                    commandId.Substring(PathTogglePrefix.Length);


                var parts =
                    payload.Split(
                        new[]
                        {
                            PathSeparator
                        },
                        StringSplitOptions.None);


                if (parts.Length == 2)
                {
                    var libraryName = parts[0];
                    var pathValue = parts[1];


                    var path =
                        config.LibraryPaths.FirstOrDefault(x =>
                            string.Equals(
                                x.LibraryName,
                                libraryName,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            string.Equals(
                                x.Path,
                                pathValue,
                                StringComparison.OrdinalIgnoreCase));


                    if (path != null)
                    {
                        path.Enabled = !path.Enabled;


                        BuildLibraryList(config);

                        this.store.SetOptions(config);

                        RaiseUIViewInfoChanged();
                    }
                }


                return Task.FromResult<IPluginUIView>(this);
            }



            return Task.FromResult<IPluginUIView>(this);
        }
    }
}