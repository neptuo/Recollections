using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoFile = System.IO.File;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/images")]
    public class ImageController : ControllerBase
    {
        private readonly DataContext dataContext;
        private readonly StorageOptions configuration;
        private readonly PathResolver pathResolver;

        public ImageController(DataContext dataContext, PathResolver pathResolver, IOptions<StorageOptions> configuration)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(pathResolver, "pathResolver");
            Ensure.NotNull(configuration, "configuration");
            this.dataContext = dataContext;
            this.pathResolver = pathResolver;
            this.configuration = configuration.Value;
        }

        private async Task<IActionResult> RunEntryAsync(string entryId, Func<Entry, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(entryId, "entryId");

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entity = await dataContext.Entries.FindAsync(entryId);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            return await handler(entity);
        }

        [HttpGet]
        public Task<IActionResult> List(string entryId) => RunEntryAsync(entryId, async entry =>
        {
            List<Image> entities = await dataContext.Images.Where(i => i.Entry.Id == entryId).ToListAsync();
            List<ImageModel> result = new List<ImageModel>();

            foreach (Image entity in entities)
            {
                var model = new ImageModel();
                MapEntityToModel(entity, model);

                result.Add(model);
            }

            return Ok(result);
        });

        [HttpGet("{imageId}")]
        public Task<IActionResult> Detail(string entryId, string imageId) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            var model = new ImageModel();
            MapEntityToModel(entity, model);

            return Ok(model);
        });

        [HttpGet("{imageId}/preview")]
        public async Task<IActionResult> FileContent(string entryId, string imageId)
        {
            Entry entry = await dataContext.Entries.FindAsync(entryId);
            if (entry == null)
                return NotFound();

            string storagePath = GetStoragePath(entry);

            Image entity = await dataContext.Images.FindAsync(imageId);
            if (entity == null)
                return NotFound();

            if (entity.Entry.Id != entryId)
                return BadRequest();

            string path = Path.Combine(storagePath, entity.FileName);
            return File(new FileStream(path, FileMode.Open), "image/png");
        }

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => RunEntryAsync(entryId, async entry =>
        {
            string imageId = Guid.NewGuid().ToString();
            string imageName = imageId + Path.GetExtension(file.FileName);

            Image entity = new Image()
            {
                Id = imageId,
                FileName = imageName,
                Entry = entry
            };

            await dataContext.Images.AddAsync(entity);

            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, imageName);

            await CopyFileAsync(file, path);
            await dataContext.SaveChangesAsync();

            ImageModel model = new ImageModel();
            MapEntityToModel(entity, model);
            return base.Ok(model);
        });

        [HttpPut("{imageId}")]
        public Task<IActionResult> Update(string entryId, string imageId, ImageModel model) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            MapModelToEntity(model, entity);

            dataContext.Images.Update(entity);
            await dataContext.SaveChangesAsync();

            return NoContent();
        });

        [HttpDelete("{imageId}")]
        public Task<IActionResult> Delete(string entryId, string imageId) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, entity.FileName);

            dataContext.Images.Remove(entity);
            await dataContext.SaveChangesAsync();

            IoFile.Delete(path);

            return Ok();
        });

        private static async Task CopyFileAsync(IFormFile file, string path)
        {
            using (FileStream target = IoFile.Create(path))
            using (Stream source = file.OpenReadStream())
                await source.CopyToAsync(target);
        }

        private string GetStoragePath(Entry entry)
        {
            string storagePath = pathResolver(configuration.GetPath(entry.UserId, entry.Id));
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }

        private void MapEntityToModel(Image entity, ImageModel model)
        {
            model.Id = entity.Id;
            model.Name = entity.Name;
            model.Description = entity.Description;

            model.Preview = $"api/entries/{entity.Entry.Id}/images/{entity.Id}/preview";
            model.Thumbnail = model.Preview;
            model.Original = model.Preview;
        }

        private void MapModelToEntity(ImageModel model, Image entity)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
        }
    }
}
