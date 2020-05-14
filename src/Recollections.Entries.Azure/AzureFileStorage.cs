using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
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

        public AzureFileStorage(IOptions<AzureStorageOptions> options)
        {
            Ensure.NotNull(options, "options");
            this.options = options.Value;
        }

        private CloudFileDirectory GetRootDirectory()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(options.ConnectionString);

            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference("entries");

            if (!share.Exists())
                throw Ensure.Exception.InvalidOperation("Missing file share.");

            CloudFileDirectory rootDir = share.GetRootDirectoryReference();
            return rootDir;
        }

        private async Task<CloudFileDirectory> GetDirectoryAsync(Entry entry)
        {
            CloudFileDirectory rootDirectory = GetRootDirectory();

            CloudFileDirectory userDirectory = rootDirectory.GetDirectoryReference(entry.UserId);
            await userDirectory.CreateIfNotExistsAsync();

            CloudFileDirectory entryDirectory = userDirectory.GetDirectoryReference(entry.Id);
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

        private async Task<CloudFile> GetFileAsync(Entry entry, Image image, ImageType type)
        {
            CloudFileDirectory entryDirectory = await GetDirectoryAsync(entry);
            string fileName = GetImageFileName(image, type);

            CloudFile imageFile = entryDirectory.GetFileReference(fileName);
            return imageFile;
        }

        public async Task DeleteAsync(Entry entry, Image image, ImageType type)
        {
            CloudFile imageFile = await GetFileAsync(entry, image, type);
            await imageFile.DeleteIfExistsAsync();
        }

        public async Task<Stream> FindAsync(Entry entry, Image image, ImageType type)
        {
            CloudFile imageFile = await GetFileAsync(entry, image, type);
            if (await imageFile.ExistsAsync())
                return await imageFile.OpenReadAsync();

            return null;
        }

        public async Task SaveAsync(Entry entry, Image image, Stream content, ImageType type)
        {
            CloudFile imageFile = await GetFileAsync(entry, image, type);
            using (CloudFileStream imageStream = await imageFile.OpenWriteAsync(content.Length))
            {
                await content.CopyToAsync(imageStream);
                await imageStream.CommitAsync();
            }
        }
    }
}
