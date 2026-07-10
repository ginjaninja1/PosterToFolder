using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace PosterToFolder.UI.Config
{
    /// <summary>
    /// Pure domain logic for reconciling the persisted library/path filter
    /// list against Emby's current library layout. No UI/visual concerns
    /// live here - see <see cref="ConfigViewBuilder"/> for that.
    ///
    /// Rule: paths/libraries that disappear from Emby are NEVER pruned from
    /// this list (they may come back later). Only currently-valid paths can
    /// be toggled from the UI - see IsPathCurrentlyValid.
    /// </summary>
    internal static class LibraryPathReconciler
    {
        /// <summary>
        /// Adds any newly discovered library paths to the persisted config.
        /// Never removes existing entries.
        /// </summary>
        public static void EnsureDiscoveredPaths(
            ConfigUI config,
            IReadOnlyList<VirtualFolderInfo> currentFolders)
        {
            if (config.LibraryPaths == null)
            {
                config.LibraryPaths = new List<LibraryPathFilterItem>();
            }

            foreach (var folder in currentFolders)
            {
                foreach (var location in folder.Locations)
                {
                    var exists = config.LibraryPaths.Any(x =>
                        string.Equals(x.Path, location, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
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

        /// <summary>
        /// True when the named library still exists and the path is still
        /// one of its current locations. False for stale/removed entries -
        /// these are not accessible from the UI even if still on disk.
        /// </summary>
        public static bool IsPathCurrentlyValid(
            IReadOnlyList<VirtualFolderInfo> currentFolders,
            string libraryName,
            string path)
        {
            var folder = currentFolders.FirstOrDefault(x =>
                string.Equals(x.Name, libraryName, StringComparison.OrdinalIgnoreCase));

            if (folder == null)
            {
                return false;
            }

            return folder.Locations.Any(loc =>
                string.Equals(loc, path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Toggles every currently-valid path for a library as a group. Stale
        /// paths under the same library name are left untouched, since they
        /// are not currently accessible.
        /// Returns true if anything changed (caller should persist).
        /// </summary>
        public static bool ToggleLibrary(
            ConfigUI config,
            IReadOnlyList<VirtualFolderInfo> currentFolders,
            string libraryName)
        {
            var validPaths = config.LibraryPaths
                .Where(x =>
                    string.Equals(x.LibraryName, libraryName, StringComparison.OrdinalIgnoreCase) &&
                    IsPathCurrentlyValid(currentFolders, libraryName, x.Path))
                .ToList();

            if (validPaths.Count == 0)
            {
                return false;
            }

            bool newState = !validPaths.Any(x => x.Enabled);

            foreach (var path in validPaths)
            {
                path.Enabled = newState;
            }

            return true;
        }

        /// <summary>
        /// Toggles a single path. No-op (returns false) if the path is stale -
        /// only currently valid paths can be toggled from the UI.
        /// </summary>
        public static bool TogglePath(
            ConfigUI config,
            IReadOnlyList<VirtualFolderInfo> currentFolders,
            string libraryName,
            string path)
        {
            if (!IsPathCurrentlyValid(currentFolders, libraryName, path))
            {
                return false;
            }

            var entry = config.LibraryPaths.FirstOrDefault(x =>
                string.Equals(x.LibraryName, libraryName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return false;
            }

            entry.Enabled = !entry.Enabled;
            return true;
        }
    }
}