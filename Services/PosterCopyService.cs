using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;

namespace PosterToFolder.Services
{
    /// <summary>
    /// Evaluates a single movie/show item and, if it has a primary ("poster") image
    /// but no folder image yet, copies the poster to folder.ext alongside it.
    /// </summary>
    public class PosterCopyService
    {
        // Windows-supported image extensions we recognize as a possible existing folder image.
        private static readonly string[] SupportedFolderImageExtensions = { ".jpg", ".png", ".gif" };

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public PosterCopyService(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        /// <summary>Evaluates one item and copies its poster to folder.ext if applicable.</summary>
        /// <param name="item">The Movie or Series to evaluate.</param>
        /// <param name="typeLabel">Display label for logging, e.g. "Movie" or "Series".</param>
        public void EvaluateAndCopy(BaseItem item, string typeLabel)
        {
            var name = item.Name ?? item.Path ?? "(unknown)";

            // ContainingFolderPath returns the item's own folder if it is folder-based (e.g. Series,
            // or a movie stored as "Movie Name/movie.mkv"), or the parent directory of the file
            // for single-file movies not stored in their own folder.
            var folderPath = item.ContainingFolderPath;

            if (string.IsNullOrEmpty(folderPath))
            {
                this.logger.Warn("{0} - {1} - Could not determine a containing folder path, skipping.", typeLabel, name);
                return;
            }

            string sourcePath = null;
            if (item.HasImage(ImageType.Primary, 0))
            {
                sourcePath = item.GetImagePath(ImageType.Primary, 0);
            }

            var sourceDisplay = string.IsNullOrEmpty(sourcePath) ? "None" : Path.GetFileName(sourcePath);

            var existingDestinationPath = this.FindExistingFolderImage(folderPath);
            var destinationDisplay = existingDestinationPath != null ? Path.GetFileName(existingDestinationPath) : "None";

            this.logger.Debug("{0} - {1} - [Source: {2}, Destination: {3}]", typeLabel, name, sourceDisplay, destinationDisplay);

            // Nothing to do if there's no poster to copy, or a folder image already exists.
            if (string.IsNullOrEmpty(sourcePath) || existingDestinationPath != null)
            {
                return;
            }

            if (!this.fileSystem.FileExists(sourcePath))
            {
                this.logger.Warn(
                    "{0} - {1} - Emby reports a primary image at {2} but the file could not be found on disk.",
                    typeLabel,
                    name,
                    sourcePath);
                return;
            }

            var extension = Path.GetExtension(sourcePath);
            var destinationPath = Path.Combine(folderPath, "folder" + extension);

            try
            {
                this.fileSystem.CopyFile(sourcePath, destinationPath, false);
                this.logger.Info("{0} - {1} - Copied {2}", typeLabel, name, Path.GetFileName(destinationPath));
            }
            catch (Exception ex)
            {
                this.logger.Warn(
                    "{0} - {1} - Failed to copy {2} to {3}: {4}",
                    typeLabel,
                    name,
                    Path.GetFileName(sourcePath),
                    Path.GetFileName(destinationPath),
                    ex.Message);
            }
        }

        private string FindExistingFolderImage(string folderPath)
        {
            foreach (var ext in SupportedFolderImageExtensions)
            {
                var candidate = Path.Combine(folderPath, "folder" + ext);
                if (this.fileSystem.FileExists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
