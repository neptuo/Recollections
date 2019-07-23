using ExifLib;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Services
{
    public class ImageService
    {
        private readonly DataContext dataContext;
        private readonly StorageOptions configuration;
        private readonly PathResolver pathResolver;
        private readonly ImageResizeService resizeService;

        public ImageService(DataContext dataContext, PathResolver pathResolver, IOptions<StorageOptions> configuration, ImageResizeService resizeService)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(pathResolver, "pathResolver");
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(resizeService, "resizeService");
            this.dataContext = dataContext;
            this.pathResolver = pathResolver;
            this.configuration = configuration.Value;
            this.resizeService = resizeService;
        }

        public string GetStoragePath(Entry entry)
        {
            string storagePath = pathResolver(configuration.GetPath(entry.UserId, entry.Id));
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }

        public async Task<Image> CreateAsync(Entry entry, IFormFile file)
        {
            string imageId = Guid.NewGuid().ToString();
            string fileName = imageId + Path.GetExtension(file.FileName);

            Validate(file);

            Image entity = new Image()
            {
                Id = imageId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = fileName,
                Created = DateTime.Now,
                When = entry.When,
                Entry = entry
            };

            await dataContext.Images.AddAsync(entity);

            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, fileName);

            await CopyFileAsync(file, path);
            SetProperties(entity, path);

            if (entity.Location.HasValue() && !entry.Locations.Contains(entity.Location))
                entry.Locations.Add(entity.Location);

            await dataContext.SaveChangesAsync();

            await ComputeOtherSizesAsync(entry, entity);

            return entity;
        }

        private void Validate(IFormFile file)
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

        private void SetProperties(Image entity, string path)
        {
            try
            {
                using (ImagePropertyReader propertyReader = new ImagePropertyReader(path))
                {
                    entity.Location.Longitude = propertyReader.GetLongitude();
                    entity.Location.Latitude = propertyReader.GetLatitude();
                    entity.Location.Altitude = propertyReader.GetAltitude();
                }
            }
            catch (ExifLibException)
            {
            }
        }

        private Task ComputeOtherSizesAsync(Entry entry, Image image)
        {
            var path = new ImagePath(this, entry, image);

            resizeService.Thumbnail(path.Original, path.Thumbnail, 200, 150);
            resizeService.Resize(path.Original, path.Preview, 1024);

            return Task.CompletedTask;
        }

        public async Task DeleteAllAsync(Entry entry)
        {
            List<Image> entities = await dataContext.Images.Where(i => i.Entry.Id == entry.Id).ToListAsync();
            foreach (var entity in entities)
                await DeleteAsync(entry, entity);
        }

        public async Task DeleteAsync(Entry entry, Image entity)
        {
            ImagePath path = new ImagePath(this, entry, entity);

            dataContext.Images.Remove(entity);
            await dataContext.SaveChangesAsync();

            File.Delete(path.Original);
            File.Delete(path.Preview);
            File.Delete(path.Thumbnail);
        }

        private static async Task CopyFileAsync(IFormFile file, string path)
        {
            using (FileStream target = File.Create(path))
            using (Stream source = file.OpenReadStream())
                await source.CopyToAsync(target);
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
