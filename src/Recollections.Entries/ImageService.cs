using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
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
        private const int PreviewWidth = 1024;
        private const int ThumbnailWidth = 200;
        private const int ThumbnailHeight = 150;

        private readonly DataContext dataContext;
        private readonly IImageValidator validator;
        private readonly IFileStorage fileStorage;
        private readonly ImageResizeService resizeService;
        private readonly FreeLimitsChecker freeLimits;

        public ImageService(DataContext dataContext, IFileStorage fileStorage, IImageValidator validator, ImageResizeService resizeService, FreeLimitsChecker freeLimits)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileStorage, "fileStorage");
            Ensure.NotNull(validator, "validator");
            Ensure.NotNull(resizeService, "resizeService");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.dataContext = dataContext;
            this.fileStorage = fileStorage;
            this.validator = validator;
            this.resizeService = resizeService;
            this.freeLimits = freeLimits;
        }

        public async Task<Image> CreateAsync(Entry entry, IFileInput file)
        {
            Ensure.NotNull(entry, "entry");
            Ensure.NotNull(file, "file");

            await validator.ValidateAsync(entry.UserId, file);

            (int width, int height) size = default;
            using (Stream imageContent = file.OpenReadStream())
                size = resizeService.GetSize(imageContent);

            string imageId = Guid.NewGuid().ToString();
            Image entity = new Image()
            {
                Id = imageId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = imageId + Path.GetExtension(file.FileName),
                Created = DateTime.Now,
                When = entry.When,
                Entry = entry,
                OriginalWidth = size.width,
                OriginalHeight = size.height
            };

            await dataContext.Images.AddAsync(entity);

            bool isOriginalStored = await freeLimits.IsOriginalImageStoredAsync(entry.UserId);

            if (isOriginalStored)
                await CopyFileAsync(file, entry, entity);

            using (Stream imageContent = file.OpenReadStream())
                SetProperties(entity, imageContent);

            await dataContext.SaveChangesAsync();

            using (Stream imageContent = file.OpenReadStream())
                await ComputeOtherSizesAsync(entry, entity, imageContent, true);

            return entity;
        }

        private void SetProperties(Image entity, Stream imageContent, bool isWhenIncluded = true)
        {
            using (ImagePropertyReader propertyReader = new ImagePropertyReader(imageContent))
            {
                if (entity.Location == null)
                    entity.Location = new();

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

            bool canSeek = originalContent.CanSeek && fileStorage.CanStreamSeek;
            await ComputeOtherSizesAsync(entry, image, originalContent, canSeek);
        }

        private async Task ComputeOtherSizesAsync(Entry entry, Image image, Stream originalContent, bool canSeek)
        {
            if (!canSeek)
            {
                var memoryStream = new MemoryStream();
                await originalContent.CopyToAsync(memoryStream);
                await originalContent.DisposeAsync();

                originalContent = memoryStream;
                originalContent.Position = 0;
                canSeek = true;
            }

            using (var thumbnailContent = new MemoryStream())
            using (var originalCopy = new MemoryStream())
            {
                await originalContent.CopyToAsync(originalCopy);
                originalCopy.Position = 0;

                resizeService.Thumbnail(originalCopy, thumbnailContent, ThumbnailWidth, ThumbnailHeight);
                thumbnailContent.Position = 0;

                await fileStorage.SaveAsync(entry, image, thumbnailContent, ImageType.Thumbnail);
            }

            if (canSeek)
                originalContent.Position = 0;
            else
                originalContent = await fileStorage.FindAsync(entry, image, ImageType.Original);

            using (var previewContent = new MemoryStream())
            using (var originalCopy = new MemoryStream())
            {
                await originalContent.CopyToAsync(originalCopy);
                originalCopy.Position = 0;

                resizeService.Resize(originalCopy, previewContent, PreviewWidth);
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

        public void MapEntityToModel(Image entity, ImageModel model, string userId)
        {
            model.Id = entity.Id;
            model.UserId = userId;
            model.Name = entity.Name;
            model.Description = entity.Description;
            model.When = entity.When;

            if (entity.Location != null)
            {
                model.Location.Latitude = entity.Location.Latitude;
                model.Location.Longitude = entity.Location.Longitude;
                model.Location.Altitude = entity.Location.Altitude;
            }

            string basePath = $"api/entries/{entity.Entry.Id}/images/{entity.Id}";

            var previewSize = resizeService.GetResizedBounds(entity.OriginalWidth, entity.OriginalHeight, PreviewWidth);
            model.Preview = new MediaSourceModel($"{basePath}/preview", previewSize.width, previewSize.height);
            model.Thumbnail = new MediaSourceModel($"{basePath}/thumbnail", ThumbnailWidth, ThumbnailHeight);
            model.Original = new MediaSourceModel($"{basePath}/original", entity.OriginalWidth, entity.OriginalHeight);
        }

        public void MapEntitiesToModels(IEnumerable<Image> entities, ICollection<ImageModel> models, string userId)
        {
            foreach (Image entity in entities)
            {
                var model = new ImageModel();
                MapEntityToModel(entity, model, userId);

                models.Add(model);
            }
        }

        public void MapModelToEntity(ImageModel model, Image entity)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.When = model.When;

            if (entity.Location == null)
                entity.Location = new();

            entity.Location.Latitude = model.Location.Latitude;
            entity.Location.Longitude = model.Location.Longitude;
            entity.Location.Altitude = model.Location.Altitude;
        }
    }
}
