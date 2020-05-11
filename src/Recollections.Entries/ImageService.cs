using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace Neptuo.Recollections.Entries
{
    public class ImageService
    {
        private readonly DataContext dataContext;
        private readonly StorageOptions configuration;
        private readonly IFileStorage fileStorage;
        private readonly ImageResizeService resizeService;

        public ImageService(DataContext dataContext, IFileStorage fileStorage, IOptions<StorageOptions> configuration, ImageResizeService resizeService)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileStorage, "fileStorage");
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(resizeService, "resizeService");
            this.dataContext = dataContext;
            this.fileStorage = fileStorage;
            this.configuration = configuration.Value;
            this.resizeService = resizeService;
        }

        public async Task<Image> CreateAsync(Entry entry, IFileInput file)
        {
            string imageId = Guid.NewGuid().ToString();

            Validate(file);

            Image entity = new Image()
            {
                Id = imageId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = imageId + Path.GetExtension(file.FileName),
                Created = DateTime.Now,
                When = entry.When,
                Entry = entry
            };

            await dataContext.Images.AddAsync(entity);

            await CopyFileAsync(file, entry, entity);
            
            using (Stream imageContnet = file.OpenReadStream())
                SetProperties(entity, imageContnet);

            await dataContext.SaveChangesAsync();

            await ComputeOtherSizesAsync(entry, entity);

            return entity;
        }

        private void Validate(IFileInput file)
        {
            if (file.Length > configuration.MaxLength)
                throw new ImageMaxLengthExceededException();

            string extension = Path.GetExtension(file.FileName);
            if (extension == null)
                throw new ImageNotSupportedExtensionException();

            extension = extension.ToLowerInvariant();
            if (!configuration.IsSupportedExtension(extension))
                throw new ImageNotSupportedExtensionException();
        }

        private void SetProperties(Image entity, Stream imageContent, bool isWhenIncluded = true)
        {
            using (ImagePropertyReader propertyReader = new ImagePropertyReader(imageContent))
            {
                entity.Location.Longitude = propertyReader.FindLongitude();
                entity.Location.Latitude = propertyReader.FindLatitude();
                entity.Location.Altitude = propertyReader.FindAltitude();

                if (isWhenIncluded)
                {
                    DateTime? when = propertyReader.FindTakenWhen();
                    if (when != null)
                        entity.When = when.Value;
                }
            }
        }

        public async Task ComputeOtherSizesAsync(Entry entry, Image image)
        {
            var originalContent = await fileStorage.FindAsync(entry, image, ImageType.Original);
            if (originalContent == null)
                return;

            using (var thumbnailContent = new MemoryStream())
            {
                resizeService.Thumbnail(originalContent, thumbnailContent, 200, 150);
                thumbnailContent.Position = 0;
                await fileStorage.SaveAsync(entry, image, thumbnailContent, ImageType.Thumbnail);
            }

            if (originalContent.CanSeek)
                originalContent.Position = 0;
            else
                originalContent = await fileStorage.FindAsync(entry, image, ImageType.Original);

            using (var previewContent = new MemoryStream())
            {
                resizeService.Resize(originalContent, previewContent, 1024);
                previewContent.Position = 0;
                await fileStorage.SaveAsync(entry, image, previewContent, ImageType.Preview);
            }
        }

        public async Task DeleteAllAsync(Entry entry)
        {
            List<Image> entities = await dataContext.Images.Where(i => i.Entry.Id == entry.Id).ToListAsync();
            foreach (var entity in entities)
                await DeleteAsync(entry, entity);
        }

        public async Task DeleteAsync(Entry entry, Image entity)
        {
            dataContext.Images.Remove(entity);
            await dataContext.SaveChangesAsync();
            await fileStorage.DeleteAsync(entry, entity, ImageType.Original);
            await fileStorage.DeleteAsync(entry, entity, ImageType.Preview);
            await fileStorage.DeleteAsync(entry, entity, ImageType.Thumbnail);
        }

        private async Task CopyFileAsync(IFileInput file, Entry entry, Image image)
        {
            using (Stream source = file.OpenReadStream())
                await fileStorage.SaveAsync(entry, image, source, ImageType.Original);
        }

        public async Task SetLocationFromOriginalAsync(Entry entry, Image image)
        {
            var imageContent = await fileStorage.FindAsync(entry, image, ImageType.Original);
            if (imageContent == null)
                return;

            SetProperties(image, imageContent, isWhenIncluded: false);

            await dataContext.SaveChangesAsync();
        }

        public void MapEntityToModel(Image entity, ImageModel model)
        {
            model.Id = entity.Id;
            model.Name = entity.Name;
            model.Description = entity.Description;
            model.When = entity.When;

            model.Location.Latitude = entity.Location.Latitude;
            model.Location.Longitude = entity.Location.Longitude;
            model.Location.Altitude = entity.Location.Altitude;

            string basePath = $"api/entries/{entity.Entry.Id}/images/{entity.Id}";
            model.Preview = $"{basePath}/preview";
            model.Thumbnail = $"{basePath}/thumbnail";
            model.Original = $"{basePath}/original";
        }

        public void MapModelToEntity(ImageModel model, Image entity)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.When = model.When;

            entity.Location.Latitude = model.Location.Latitude;
            entity.Location.Longitude = model.Location.Longitude;
            entity.Location.Altitude = model.Location.Altitude;
        }
    }
}
