using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class SystemIoFileStorage : IFileStorage
    {
        private readonly SystemIoStorageOptions configuration;
        private readonly PathResolver pathResolver;
        private readonly ImageFormatDefinition formatDefinition;

        public bool CanStreamSeek => true;

        public SystemIoFileStorage(PathResolver pathResolver, IOptions<SystemIoStorageOptions> configuration, ImageFormatDefinition formatDefinition)
        {
            Ensure.NotNull(pathResolver, "pathResolver");
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(formatDefinition, "formatDefinition");
            this.pathResolver = pathResolver;
            this.configuration = configuration.Value;
            this.formatDefinition = formatDefinition;
        }

        public ImagePath GetPath(Entry entry, Image entity)
            => new ImagePath(this, formatDefinition, entry, entity);

        public string GetStoragePath(Entry entry)
        {
            string storagePath = pathResolver(configuration.GetPath(entry.UserId, entry.Id));
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }

        public Task<Stream> FindAsync(Entry entry, Image image, ImageType type)
        {
            ImagePath path = GetPath(entry, image);
            string filePath = path.Get(type);
            if (!File.Exists(filePath))
                return Task.FromResult<Stream>(null);

            return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open));
        }

        public async Task SaveAsync(Entry entry, Image image, Stream content, ImageType type)
        {
            ImagePath path = GetPath(entry, image);
            string filePath = path.Get(type);
            using (FileStream target = File.Create(filePath))
                await content.CopyToAsync(target);
        }

        public Task DeleteAsync(Entry entry, Image image, ImageType type)
        {
            ImagePath path = GetPath(entry, image);
            string filePath = path.Get(type);
            File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}
