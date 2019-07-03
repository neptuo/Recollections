using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Entries.Services;
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
        private readonly ImageService service;
        private readonly DataContext dataContext;
        private readonly StorageOptions configuration;
        private readonly PathResolver pathResolver;

        public ImageController(ImageService service, DataContext dataContext, PathResolver pathResolver, IOptions<StorageOptions> configuration)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(pathResolver, "pathResolver");
            Ensure.NotNull(configuration, "configuration");
            this.service = service;
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
                service.MapEntityToModel(entity, model);

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
            service.MapEntityToModel(entity, model);

            return Ok(model);
        });

        [HttpGet("{imageId}/preview")]
        public Task<IActionResult> FileContentPreview(string entryId, string imageId)
            => GetFileContent(entryId, imageId, "preview");

        [HttpGet("{imageId}/thumbnail")]
        public Task<IActionResult> FileContentThumbnail(string entryId, string imageId)
            => GetFileContent(entryId, imageId, "thumbnail");

        [HttpGet("{imageId}/original")]
        public Task<IActionResult> FileContent(string entryId, string imageId)
            => GetFileContent(entryId, imageId, null);

        private async Task<IActionResult> GetFileContent(string entryId, string imageId, string type)
        {
            Entry entry = await dataContext.Entries.FindAsync(entryId);
            if (entry == null)
                return NotFound();

            string storagePath = service.GetStoragePath(entry);

            Image entity = await dataContext.Images.FindAsync(imageId);
            if (entity == null)
                return NotFound();

            if (entity.Entry.Id != entryId)
                return BadRequest();

            string fileName = entity.FileName;
            if (type != null)
            {
                string extension = Path.GetExtension(fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName);
                fileName = String.Concat(fileName, ".", type, extension);
            }

            string path = Path.Combine(storagePath, fileName);
            if (!IoFile.Exists(path))
                return NotFound();

            return File(new FileStream(path, FileMode.Open), "image/png");
        }

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await service.CreateAsync(entry, file);

            ImageModel model = new ImageModel();
            service.MapEntityToModel(entity, model);

            return base.Ok(model);
        });

        [HttpPut("{imageId}")]
        public Task<IActionResult> Update(string entryId, string imageId, ImageModel model) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            service.MapModelToEntity(model, entity);

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

            await service.DeleteAsync(entry, entity);

            return Ok();
        });
    }
}
