﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Entries.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
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

        public ImageController(ImageService service, DataContext dataContext)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(dataContext, "dataContext");
            this.service = service;
            this.dataContext = dataContext;
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
            => GetFileContent(entryId, imageId, ImageType.Preview);

        [HttpGet("{imageId}/thumbnail")]
        public Task<IActionResult> FileContentThumbnail(string entryId, string imageId)
            => GetFileContent(entryId, imageId, ImageType.Thumbnail);

        [HttpGet("{imageId}/original")]
        public Task<IActionResult> FileContent(string entryId, string imageId)
            => GetFileContent(entryId, imageId, ImageType.Original);

        private async Task<IActionResult> GetFileContent(string entryId, string imageId, ImageType type)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Entry entry = await dataContext.Entries.FindAsync(entryId);
            if (entry == null)
                return NotFound();

            Image entity = await dataContext.Images.FindAsync(imageId);
            if (entity == null)
                return NotFound();

            if (entity.Entry.Id != entryId)
                return BadRequest();

            ImagePath path = new ImagePath(service, entry, entity);
            string filePath = path.Get(type);
            if (!IoFile.Exists(filePath))
                return NotFound();

            if (type == ImageType.Original)
            {
                ContentDisposition header = new ContentDisposition
                {
                    FileName = entity.Name + Path.GetExtension(path.Original),
                    Inline = false
                };
                Response.Headers.Add("Content-Disposition", header.ToString());
            }

            return File(new FileStream(filePath, FileMode.Open), GetFileContentType(filePath));
        }

        private static string GetFileContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string contentType))
                contentType = "application/octet-stream";

            return contentType;
        }

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => RunEntryAsync(entryId, async entry =>
        {
            try
            {
                Image entity = await service.CreateAsync(entry, file);

                ImageModel model = new ImageModel();
                service.MapEntityToModel(entity, model);

                return Ok(model);
            }
            catch (ImageUploadValidationException)
            {
                return BadRequest();
            }
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

        [HttpPost("{imageId}/set-location-from-original")]
        public Task<IActionResult> SetLocationFromOriginal(string entryId, string imageId) => RunEntryAsync(entryId, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            await service.SetLocationFromOriginalAsync(entry, entity);

            return NoContent();
        });
    }
}
