using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neptuo;
using Neptuo.Recollections.Accounts;
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
        private const int CacheSeconds = 365 * 24 * 60 * 60;
        private static readonly StringValues CacheHeaderValue = new StringValues(new[] { "private", $"max-age={CacheSeconds}" });

        private readonly ImageService service;
        private readonly DataContext dataContext;
        private readonly IFileStorage fileProvider;
        private readonly ShareStatusService shareStatus;
        private readonly IUserNameProvider userNames;
        private readonly FreeLimitsChecker freeLimits;

        public ImageController(ImageService service, DataContext dataContext, IFileStorage fileProvider, ShareStatusService shareStatus, IUserNameProvider userNames, FreeLimitsChecker freeLimits)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileProvider, "fileProvider");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.service = service;
            this.dataContext = dataContext;
            this.fileProvider = fileProvider;
            this.shareStatus = shareStatus;
            this.userNames = userNames;
            this.freeLimits = freeLimits;
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
        [ProducesDefaultResponseType(typeof(AuthorizedModel<ImageModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Detail(string entryId, string imageId) => RunEntryAsync(entryId, Permission.Read, async (entry, permission) =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            var model = new ImageModel();
            service.MapEntityToModel(entity, model, entry.UserId);

            var result = new AuthorizedModel<ImageModel>(model)
            {
                OwnerId = entry.UserId,
                OwnerName = await userNames.GetUserNameAsync(entry.UserId),
                UserPermission = permission
            };

            return Ok(result);
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

            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch.ToString() == imageId)
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
                Response.Headers[HeaderNames.ContentDisposition] = header.ToString();
            }
            else
            {
                Response.Headers[HeaderNames.CacheControl] = CacheHeaderValue;
            }

            Response.Headers[HeaderNames.ETag] = imageId;

            return File(content, GetFileContentType(imageName));
        });

        private static string GetFileContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string contentType))
                contentType = "application/octet-stream";

            return contentType;
        }

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            string userId = HttpContext.User.FindUserId();
            if (!await freeLimits.CanCreateImageAsync(userId, entryId))
                return PremiumRequired();

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
        public Task<IActionResult> Update(string entryId, string imageId, ImageModel model) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
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
        public Task<IActionResult> Delete(string entryId, string imageId) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            await service.DeleteAsync(entry, entity);

            return Ok();
        });

        [HttpPost("{imageId}/set-location-from-original")]
        public Task<IActionResult> SetLocationFromOriginal(string entryId, string imageId) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            Image entity = await dataContext.Images.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == imageId);
            if (entity == null)
                return NotFound();

            await service.SetLocationFromOriginalAsync(entry, entity);

            return NoContent();
        });
    }
}
