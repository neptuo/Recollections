using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class AzureFileStorage : IFileStorage
    {
        private readonly AzureStorageOptions options;

        public bool CanStreamSeek => false;

        public AzureFileStorage(IOptions<AzureStorageOptions> options)
        {
            Ensure.NotNull(options, "options");
            this.options = options.Value;
        }

        private ShareDirectoryClient GetRootDirectory()
        {
            ShareClient share = new ShareClient(options.ConnectionString, options.FileShareName ?? "entries");

            if (!share.Exists())
                throw Ensure.Exception.InvalidOperation("Missing file share.");

            ShareDirectoryClient rootDir = share.GetRootDirectoryClient();
            return rootDir;
        }

        private async Task<ShareDirectoryClient> GetDirectoryAsync(Entry entry)
        {
            ShareDirectoryClient rootDirectory = GetRootDirectory();

            ShareDirectoryClient userDirectory = rootDirectory.GetSubdirectoryClient(entry.UserId);
            await userDirectory.CreateIfNotExistsAsync();

            ShareDirectoryClient entryDirectory = userDirectory.GetSubdirectoryClient(entry.Id);
            await entryDirectory.CreateIfNotExistsAsync();

            return entryDirectory;
        }

        private string GetImageFileName(Image image, ImageType type)
        {
            string AddSuffix(string name, string suffix)
                => Path.GetFileNameWithoutExtension(name) + suffix + Path.GetExtension(name);

            switch (type)
            {
                case ImageType.Original:
                    return image.FileName;
                case ImageType.Preview:
                    return AddSuffix(image.FileName, ".preview");
                case ImageType.Thumbnail:
                    return AddSuffix(image.FileName, ".thumbnail");
                default:
                    throw Ensure.Exception.NotSupported(type);
            }
        }

        private async Task<ShareFileClient> GetFileAsync(Entry entry, Image image, ImageType type)
        {
            ShareDirectoryClient entryDirectory = await GetDirectoryAsync(entry);
            string fileName = GetImageFileName(image, type);

            ShareFileClient imageFile = entryDirectory.GetFileClient(fileName);
            return imageFile;
        }

        public async Task DeleteAsync(Entry entry, Image image, ImageType type)
        {
            ShareFileClient imageFile = await GetFileAsync(entry, image, type);
            await imageFile.DeleteIfExistsAsync();
        }

        public async Task<Stream> FindAsync(Entry entry, Image image, ImageType type)
        {
            ShareFileClient imageFile = await GetFileAsync(entry, image, type);
            if (await imageFile.ExistsAsync())
            {
                ShareFileDownloadInfo download = await imageFile.DownloadAsync();
                return download.Content;
            }

            return null;
        }

        public async Task SaveAsync(Entry entry, Image image, Stream content, ImageType type)
        {
            ShareFileClient imageFile = await GetFileAsync(entry, image, type);
            await imageFile.CreateAsync(content.Length);
            
            // Reset stream position if it supports seeking
            if (content.CanSeek)
                content.Position = 0;
                
            await imageFile.UploadAsync(content);
        }

        private const string DerivedImageExtension = ".jpg";

        private static string GetVideoFileName(Video video, VideoType type)
        {
            string baseName = Path.GetFileNameWithoutExtension(video.FileName);

            switch (type)
            {
                case VideoType.Original:
                    return video.FileName;
                case VideoType.Preview:
                    return string.Concat(baseName, ".preview", DerivedImageExtension);
                case VideoType.Thumbnail:
                    return string.Concat(baseName, ".thumbnail", DerivedImageExtension);
                default:
                    throw Ensure.Exception.NotSupported(type);
            }
        }

        private async Task<ShareFileClient> GetFileAsync(Entry entry, Video video, VideoType type)
        {
            ShareDirectoryClient entryDirectory = await GetDirectoryAsync(entry);
            string fileName = GetVideoFileName(video, type);
            return entryDirectory.GetFileClient(fileName);
        }

        public async Task DeleteAsync(Entry entry, Video video, VideoType type)
        {
            ShareFileClient file = await GetFileAsync(entry, video, type);
            await file.DeleteIfExistsAsync();
        }

        public async Task<Stream> FindAsync(Entry entry, Video video, VideoType type)
        {
            ShareFileClient file = await GetFileAsync(entry, video, type);
            if (await file.ExistsAsync())
            {
                ShareFileDownloadInfo download = await file.DownloadAsync();
                return download.Content;
            }

            return null;
        }

        public async Task SaveAsync(Entry entry, Video video, Stream content, VideoType type)
        {
            ShareFileClient file = await GetFileAsync(entry, video, type);
            await file.CreateAsync(content.Length);

            if (content.CanSeek)
                content.Position = 0;

            await file.UploadAsync(content);
        }
    }
}
