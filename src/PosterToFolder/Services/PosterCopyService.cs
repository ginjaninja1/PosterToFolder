using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PosterToFolder.Services
{
    public enum EvaluationResult
    {
        Copied,
        Skipped,
        Errored
    }

    /// <summary>
    /// Evaluates a single movie/show item and, if it has a primary ("poster") image
    /// but no folder image yet, copies the poster to folder.ext alongside it.
    /// </summary>
    public class PosterCopyService
    {
        // Windows-supported image extensions recognized as a possible existing folder image.
        private static readonly string[] SupportedFolderImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly IImageProcessor imageProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PosterCopyService"/> class.
        /// </summary>
        public PosterCopyService(ILogger logger, IFileSystem fileSystem, IImageProcessor imageProcessor)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Evaluates one item and copies/converts its poster to folder.jpg or folder.ext if applicable.
        /// </summary>
        /// <param name="item">The Movie or Series to evaluate.</param>
        /// <param name="typeLabel">Display label for logging, e.g. "Movie" or "Series".</param>
        /// <param name="cancellationToken">A cancellation token from the scheduled task environment.</param>
        /// <returns>An EvaluationResult indicating if the item was copied, skipped, or threw an error.</returns>
        public async Task<EvaluationResult> EvaluateAndCopyAsync(BaseItem item, string typeLabel, CancellationToken cancellationToken)
        {
            var name = item.Name ?? item.Path ?? "(unknown)";
            var clientId = item.GetClientId();

            var containingFolderPathRaw = item.ContainingFolderPath;
            var folderPath = containingFolderPathRaw;

            if (string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(item.Path))
            {
                folderPath = this.fileSystem.DirectoryExists(item.Path)
                    ? item.Path
                    : Path.GetDirectoryName(item.Path);
            }

            var hasPrimaryImage = item.HasImage(ImageType.Primary, 0);
            ItemImageInfo primaryImageInfo = null;
            if (hasPrimaryImage)
            {
                primaryImageInfo = item.GetImageInfo(ImageType.Primary, 0);
            }

            var sourcePath = primaryImageInfo?.Path;
            var sourceIsLocalFile = primaryImageInfo?.IsLocalFile;
            var sourceExistsOnDisk = !string.IsNullOrEmpty(sourcePath) && this.fileSystem.FileExists(sourcePath);

            var sourceDirectory = string.IsNullOrEmpty(sourcePath) ? null : Path.GetDirectoryName(sourcePath);
            var sourceIsInItemFolder = !string.IsNullOrEmpty(sourceDirectory)
                && !string.IsNullOrEmpty(folderPath)
                && string.Equals(sourceDirectory.TrimEnd('\\', '/'), folderPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

            string sourceLocation;
            if (string.IsNullOrEmpty(sourcePath))
            {
                sourceLocation = "None";
            }
            else if (sourceIsInItemFolder)
            {
                sourceLocation = "MovieFolder";
            }
            else
            {
                sourceLocation = "Metadata";
            }

            string internalMetadataPath;
            try
            {
                internalMetadataPath = item.GetInternalMetadataPath();
            }
            catch (Exception ex)
            {
                internalMetadataPath = "(error reading GetInternalMetadataPath: " + ex.Message + ")";
            }

            var existingDestinationPath = string.IsNullOrEmpty(folderPath) ? null : this.FindExistingFolderImage(folderPath);

            this.logger.Debug(
                "{0} - {1} - [Id: {2}, Path: {3}, ContainingFolderPathRaw: {4}, ResolvedFolder: {5}, HasPrimaryImage: {6}, SourcePath: {7}, SourceLocation: {8}, SourceExistsOnDisk: {9}, InternalMetadataPath: {10}, ExistingDestination: {11}]",
                typeLabel,
                name,
                clientId,
                item.Path ?? "(null)",
                containingFolderPathRaw ?? "(null)",
                folderPath ?? "(null)",
                hasPrimaryImage,
                sourcePath ?? "(null)",
                sourceLocation,
                sourceExistsOnDisk,
                internalMetadataPath ?? "(null)",
                existingDestinationPath ?? "(none)");

            if (string.IsNullOrEmpty(folderPath))
            {
                this.logger.Warn("{0} - {1} - [Id: {2}] Could not determine a containing folder path, skipping. See Debug line above for full detail.", typeLabel, name, clientId);
                return EvaluationResult.Skipped;
            }

            if (!hasPrimaryImage || string.IsNullOrEmpty(sourcePath) || existingDestinationPath != null)
            {
                return EvaluationResult.Skipped;
            }

            if (!sourceExistsOnDisk)
            {
                this.logger.Warn(
                    "{0} - {1} - [Id: {2}] Emby reports a primary image at {3} (IsLocalFile={4}) but the file could not be found on disk.",
                    typeLabel,
                    name,
                    clientId,
                    sourcePath,
                    sourceIsLocalFile);
                return EvaluationResult.Errored;
            }

            var extension = Path.GetExtension(sourcePath) ?? string.Empty;

            // Format-agnostic check: see if the file extension is already natively a JPEG variant
            bool isAlreadyJpg = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

            // Enforce destination filename to folder.jpg if it needs conversion to ensure Windows compatibility
            string destinationName = isAlreadyJpg ? "folder" + extension : "folder.jpg";
            var destinationPath = Path.Combine(folderPath, destinationName);

            try
            {
                if (!isAlreadyJpg)
                {
                    // 1. Configure options based on your ImageProcessingOptions properties
                    var options = new ImageProcessingOptions
                    {
                        Item = item,
                        Image = primaryImageInfo,
                        SupportedOutputFormats = new[] { ImageFormat.Jpg }, // Fixes CS0117
                        Quality = 90
                    };

                    // 2. Open a stream to the destination file
                    using (var destinationStream = File.Create(destinationPath))
                    {
                        // 3. Call the 3-argument ProcessImage method (Fixes CS1501)
                        // This instructs Emby to write the converted JPEG directly to folder.jpg
                        await this.imageProcessor.ProcessImage(options, destinationStream, cancellationToken).ConfigureAwait(false);
                    }

                    this.logger.Info("{0} - {1} - Converted format {2} to {3}", typeLabel, name, extension, destinationName);
                }
                else
                {
                    // High-speed block file copy if the file is already natively a JPEG variant
                    this.fileSystem.CopyFile(sourcePath, destinationPath, false);
                    this.logger.Info("{0} - {1} - Copied native {2}", typeLabel, name, destinationName);
                }

                return EvaluationResult.Copied;
            }
            catch (Exception ex)
            {
                this.logger.Warn(
                    "{0} - {1} - [Id: {2}] Failed to write {3} from {4}: {5}",
                    typeLabel,
                    name,
                    clientId,
                    destinationName,
                    Path.GetFileName(sourcePath),
                    ex.Message);
                return EvaluationResult.Errored;
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
