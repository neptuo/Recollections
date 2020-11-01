using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/images")]
    public class ImageController : ControllerBase
    {
        private readonly ImageService service;
        private readonly DataContext dataContext;
        private readonly IFileStorage fileProvider;
        private readonly ShareStatusService shareStatus;

        public ImageController(ImageService service, DataContext dataContext, IFileStorage fileProvider, ShareStatusService shareStatus)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileProvider, "fileProvider");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.service = service;
            this.dataContext = dataContext;
            this.fileProvider = fileProvider;
            this.shareStatus = shareStatus;
        }

        [HttpGet]
        public Task<IActionResult> List(string entryId) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            List<Image> entities = await dataContext.Images
                .Where(i => i.Entry.Id == entryId)
                .OrderBy(i => i.When)
                .ToListAsync();

            List<ImageModel> result = new List<ImageModel>();
            foreach (Image entity in entities)
            {
                var model = new ImageModel();
                service.MapEntityToModel(entity, model, entry.UserId);

                result.Add(model);
            }

            return Ok(result);
        });

        [HttpGet("{imageId}")]
        public Task<IActionResult> Detail(string entryId, string imageId) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            var model = new ImageModel();
            service.MapEntityToModel(entity, model, entry.UserId);

            var permission = model.UserId == User.FindUserId() || await shareStatus.IsEntrySharedForWriteAsync(entryId, User.FindUserId()) ? Permission.Write : Permission.Read;
            Response.Headers.Add(PermissionHeader.Name, permission.ToString());

            return Ok(model);
        });

        [HttpGet("{imageId}/preview")]
        public Task<IActionResult> FileContentPreview(string entryId, string imageId)
            => GetFileContent(entryId, imageId, ImageType.Preview);

        [HttpGet("{imageId}/thumbnail")]
        public Task<IActionResult> FileContentThumbnail(string entryId, string imageId)
            => GetFileContent(entryId, imageId, ImageType.Thumbnail);

        [HttpGet("{imageId}/original")]
        public Task<IActionResult> FileContent(string entryId, string imageId)
            => GetFileContent(entryId, imageId, ImageType.Original);

        private Task<IActionResult> GetFileContent(string entryId, string imageId, ImageType type) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            Image entity = await dataContext.Images.FindAsync(imageId);
            if (entity == null)
                return NotFound();

            if (entity.Entry.Id != entryId)
                return BadRequest();

            var headers = Request.GetTypedHeaders();
            if (headers.IfModifiedSince != null)
                return StatusCode(304);

            Stream content = await fileProvider.FindAsync(entry, entity, type);
            if (content == null)
                return NotFound();

            string imageName = entity.Name + Path.GetExtension(entity.FileName);
            if (type == ImageType.Original)
            {
                ContentDisposition header = new ContentDisposition
                {
                    FileName = imageName,
                    Inline = false
                };
                Response.Headers.Add("Content-Disposition", header.ToString());
            }

            Response.Headers.Add("ETag", imageId);

            return File(content, GetFileContentType(imageName), entity.Created, null);
        });

        private static string GetFileContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string contentType))
                contentType = "application/octet-stream";

            return contentType;
        }

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => RunEntryAsync(entryId, Permission.Write, async entry =>
        {
            try
            {
                Image entity = await service.CreateAsync(entry, new FormFileInput(file));

                ImageModel model = new ImageModel();
                service.MapEntityToModel(entity, model, entry.UserId);

                return Ok(model);
            }
            catch (ImageUploadValidationException)
            {
                return BadRequest();
            }
        });

        [HttpPut("{imageId}")]
        public Task<IActionResult> Update(string entryId, string imageId, ImageModel model) => RunEntryAsync(entryId, Permission.Write, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            service.MapModelToEntity(model, entity);

            dataContext.Entry(entity).State = EntityState.Modified;
            dataContext.Entry(entity.Location).State = EntityState.Modified;
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

        [HttpPost("{imageId}/set-location-from-original")]
        public Task<IActionResult> SetLocationFromOriginal(string entryId, string imageId) => RunEntryAsync(entryId, Permission.Write, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            await service.SetLocationFromOriginalAsync(entry, entity);

            return NoContent();
        });
    }
}
