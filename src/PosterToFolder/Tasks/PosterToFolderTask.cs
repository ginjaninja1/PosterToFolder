using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using PosterToFolder.Services;
using PosterToFolder.UI.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PosterToFolder.Tasks
{
    /// <summary>
    /// Scheduled task that finds movies and TV shows which have a poster (primary) image
    /// but no folder.ext yet, and copies the poster to folder.ext alongside it.
    /// </summary>
    public class PosterToFolderTask : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        public PosterToFolderTask(ILibraryManager libraryManager, IFileSystem fileSystem, ILogManager logManager)
        {
            this.libraryManager = libraryManager;
            this.fileSystem = fileSystem;
            this.logger = logManager.GetLogger("PosterToFolder");
        }

        public string Name => "Copy Posters to Folder Images";

        public string Key => "PosterToFolderTask";

        public string Description => "Finds movies and TV shows with a poster image but no folder image, and copies the poster to folder.ext.";

        public string Category => "Library";

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var options = Plugin.Instance.Configuration;

            if (!options.EnablePlugin)
            {
                this.logger.Info("Poster To Folder is disabled in plugin settings. Exiting without processing.");
                return Task.CompletedTask;
            }

            this.SyncLibraryPathFilters(options);

            var filterRows = options.LibraryPaths.ToList();
            var enabledPaths = filterRows.Where(p => p.Enabled && !string.IsNullOrEmpty(p.Path)).Select(p => p.Path).ToList();
            var disabledPaths = filterRows.Where(p => !p.Enabled && !string.IsNullOrEmpty(p.Path)).Select(p => p.Path).ToList();

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Movie).Name, typeof(Series).Name },
                Recursive = true,
                DtoOptions = new DtoOptions(true),
                IsVirtualItem = false, // exclude placeholders (e.g. "Coming Soon" items with no real file yet)
            };

            var items = this.libraryManager.GetItemList(query).ToList();
            var copyService = new PosterCopyService(this.logger, this.fileSystem);

            var total = items.Count;
            var processed = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (this.IsInScope(item, enabledPaths, disabledPaths))
                {
                    var typeLabel = item is Series ? "Series" : "Movie";
                    copyService.EvaluateAndCopy(item, typeLabel);
                }

                processed++;
                progress.Report(total == 0 ? 100.0 : (processed / (double)total) * 100.0);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines whether an item's folder falls under an enabled path.
        /// Rules: disabled paths always exclude; if any enabled paths are configured, the item
        /// must fall under one of them; if no filters are configured at all, everything is in scope.
        /// </summary>
        private bool IsInScope(BaseItem item, List<string> enabledPaths, List<string> disabledPaths)
        {
            if (enabledPaths.Count == 0 && disabledPaths.Count == 0)
            {
                return true;
            }

            var itemPath = item.ContainingFolderPath ?? item.Path;

            if (string.IsNullOrEmpty(itemPath))
            {
                return true;
            }

            if (disabledPaths.Any(p => IsUnderPath(itemPath, p)))
            {
                return false;
            }

            if (enabledPaths.Count > 0)
            {
                return enabledPaths.Any(p => IsUnderPath(itemPath, p));
            }

            return true;
        }

        private static bool IsUnderPath(string itemPath, string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            var normalizedRoot = root.TrimEnd('\\', '/');
            return itemPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Refreshes the config's LibraryPaths list from the server's actual current
        /// (relevant - movies/TV shows only, see RelevantLibraryTypes) libraries/paths,
        /// adding any new ones as Enabled = true. Existing rows (and their toggle state)
        /// are left untouched. Persists changes back to disk when anything is added.
        ///
        /// Delegates to the same LibraryPathReconciler the config page uses, so this
        /// task and the UI can never drift out of sync on what counts as a valid path.
        /// </summary>
        private void SyncLibraryPathFilters(PosterToFolder.Configuration.PluginConfiguration options)
        {
            try
            {
                var relevantFolders = RelevantLibraryTypes.Filter(this.libraryManager.GetVirtualFolders());

                var before = options.LibraryPaths.Count;

                LibraryPathReconciler.EnsureDiscoveredPaths(options, relevantFolders);

                if (options.LibraryPaths.Count != before)
                {
                    Plugin.Instance.SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                this.logger.Warn("Failed to sync library/path filter list: {0}", ex.Message);
            }
        }

        /*
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerWeekly,
                    DayOfWeek = DayOfWeek.Sunday,
                    TimeOfDayTicks = TimeSpan.FromHours(23).Ticks, // Sunday night
                },
            };
        }
        */
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}