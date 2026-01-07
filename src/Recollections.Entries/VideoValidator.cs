using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class VideoValidator : IVideoValidator
    {
        private readonly StorageOptions configuration;

        public VideoValidator(IOptions<StorageOptions> configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            this.configuration = configuration.Value;
        }

        public Task ValidateAsync(string userId, IFileInput file)
        {
            if (file.Length > configuration.Videos.MaxLength)
                throw new VideoMaxLengthExceededException();

            string extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (extension == null || !configuration.Videos.IsSupportedExtension(extension))
                throw new VideoNotSupportedExtensionException();

            return Task.CompletedTask;
        }
    }
}
