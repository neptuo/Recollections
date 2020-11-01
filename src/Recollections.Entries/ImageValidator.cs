using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageValidator : IImageValidator
    {
        private readonly StorageOptions configuration;

        public ImageValidator(IOptions<StorageOptions> configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            this.configuration = configuration.Value;
        }

        public async Task ValidateAsync(string userId, IFileInput file)
        {
            if (!await IsValidSizeAsync(userId, file.Length))
                throw new ImageMaxLengthExceededException();

            string extension = Path.GetExtension(file.FileName);
            if (!await IsValidExtensionAsync(userId, extension))
                throw new ImageNotSupportedExtensionException();
        }

        protected virtual Task<bool> IsValidSizeAsync(string userId, long fileLength)
            => Task.FromResult(fileLength <= configuration.MaxLength);

        protected virtual Task<bool> IsValidExtensionAsync(string userId, string extension)
        {
            if (extension == null)
                return Task.FromResult(false);

            extension = extension.ToLowerInvariant();
            if (!configuration.IsSupportedExtension(extension))
                return Task.FromResult(false);

            return Task.FromResult(true);
        }
    }
}
