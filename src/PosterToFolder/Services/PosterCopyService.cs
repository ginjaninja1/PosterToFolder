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
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;

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
        private static readonly string[] SupportedFolderImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        // Clean constructor: We completely remove IImageProcessor and IImageEncoder dependencies
        public PosterCopyService(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

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
            var sourceExistsOnDisk = !string.IsNullOrEmpty(sourcePath) && this.fileSystem.FileExists(sourcePath);

            if (string.IsNullOrEmpty(folderPath) || !hasPrimaryImage || string.IsNullOrEmpty(sourcePath) || this.FindExistingFolderImage(folderPath) != null)
            {
                return EvaluationResult.Skipped;
            }

            if (!sourceExistsOnDisk)
            {
                this.logger.Warn("{0} - {1} - [Id: {2}] Primary image file not found on disk: {3}", typeLabel, name, clientId, sourcePath);
                return EvaluationResult.Errored;
            }

            var extension = Path.GetExtension(sourcePath) ?? string.Empty;
            bool isAlreadyJpg = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

            string destinationName = isAlreadyJpg ? "folder" + extension : "folder.jpg";
            var destinationPath = Path.Combine(folderPath, destinationName);

            try
            {
                if (!isAlreadyJpg)
                {
                    // Pass the sourcePath string directly to SKCodec to open the file handle internally
                    using (var codec = SKCodec.Create(sourcePath))
                    {
                        if (codec == null)
                        {
                            throw new InvalidDataException("Failed to create SKCodec. The source image file may be corrupt.");
                        }

                        // Decode the source file into memory bitmap arrays
                        using (var bitmap = SKBitmap.Decode(codec))
                        using (var image = SKImage.FromBitmap(bitmap))
                        {
                            // Encode memory bitmap arrays directly into a JPEG output stream
                            using (var outputStream = File.Create(destinationPath))
                            {
                                image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(outputStream);
                            }
                        }
                    }

                    this.logger.Info("{0} - {1} - Successfully transcoded {2} to {3} via SkiaSharp", typeLabel, name, extension, destinationName);
                }
                else
                {
                    this.fileSystem.CopyFile(sourcePath, destinationPath, false);
                    this.logger.Info("{0} - {1} - Copied native {2}", typeLabel, name, destinationName);
                }

                return EvaluationResult.Copied;
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(destinationPath)) { File.Delete(destinationPath); }
                }
                catch { }

                this.logger.Warn("{0} - {1} - [Id: {2}] Failed to write {3} from {4}: {5}", typeLabel, name, clientId, destinationName, Path.GetFileName(sourcePath), ex.Message);
                return EvaluationResult.Errored;
            }
        }

        private string FindExistingFolderImage(string folderPath)
        {
            foreach (var ext in SupportedFolderImageExtensions)
            {
                var candidate = Path.Combine(folderPath, "folder" + ext);
                if (this.fileSystem.FileExists(candidate)) { return candidate; }
            }
            return null;
        }
    }
}
