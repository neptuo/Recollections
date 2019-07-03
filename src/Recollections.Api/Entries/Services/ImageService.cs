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
            string imageName = imageId + Path.GetExtension(file.FileName);

            Image entity = new Image()
            {
                Id = imageId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = imageName,
                Created = DateTime.Now,
                When = entry.When,
                Entry = entry
            };

            await dataContext.Images.AddAsync(entity);

            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, imageName);

            await CopyFileAsync(file, path);
            await dataContext.SaveChangesAsync();

            await ComputeOtherSizesAsync(path);

            return entity;
        }

        private Task ComputeOtherSizesAsync(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            string thumbnailPath = Path.Combine(directoryPath, String.Concat(fileName, ".thumbnail", extension));
            string previewPath = Path.Combine(directoryPath, String.Concat(fileName, ".preview", extension));

            resizeService.Thumbnail(path, thumbnailPath, 200, 150);
            resizeService.Thumbnail(path, previewPath, 800, 600);

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
            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, entity.FileName);

            dataContext.Images.Remove(entity);
            await dataContext.SaveChangesAsync();

            File.Delete(path);
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
        }
    }
}
