using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/videos")]
    public class VideoController : ControllerBase
    {
        private const int CacheSeconds = 365 * 24 * 60 * 60;
        private static readonly StringValues CacheHeaderValue = new StringValues(new[] { "private", $"max-age={CacheSeconds}" });

        private readonly DataContext db;
        private readonly IFileStorage fileStorage;
        private readonly VideoService service;
        private readonly IUserNameProvider userNames;
        
        public VideoController(VideoService service, DataContext db, IFileStorage fileStorage, ShareStatusService shareStatus, IUserNameProvider userNames)
            : base(db, shareStatus)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(fileStorage, "fileStorage");
            this.service = service;
            this.db = db;
            this.fileStorage = fileStorage;
            this.userNames = userNames;
        }

        [HttpGet("{videoId}")]
        [ProducesDefaultResponseType(typeof(AuthorizedModel<VideoModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Detail(string entryId, string videoId) => RunEntryAsync(entryId, Permission.Read, async (entry, permission) =>
        {
            Video entity = await db.Videos.FirstOrDefaultAsync(i => i.Entry.Id == entryId && i.Id == videoId);
            if (entity == null)
                return NotFound();

            var model = new VideoModel();
            service.MapEntityToModel(entity, model, entry.UserId);

            var result = new AuthorizedModel<VideoModel>(model)
            {
                OwnerId = entry.UserId,
                OwnerName = await userNames.GetUserNameAsync(entry.UserId),
                UserPermission = permission
            };

            return Ok(result);
        });

        [HttpGet("{videoId}/thumbnail")]
        public Task<IActionResult> FileContentThumbnail(string entryId, string videoId)
            => GetFileContent(entryId, videoId, VideoType.Thumbnail, enableRangeProcessing: false, inline: true);

        [HttpGet("{videoId}/preview")]
        public Task<IActionResult> FileContentPreview(string entryId, string videoId)
            => GetFileContent(entryId, videoId, VideoType.Preview, enableRangeProcessing: false, inline: true);

        [HttpGet("{videoId}/original")]
        public Task<IActionResult> FileContentOriginal(string entryId, string videoId)
            => GetFileContent(entryId, videoId, VideoType.Original, enableRangeProcessing: true, inline: true);

        private Task<IActionResult> GetFileContent(string entryId, string videoId, VideoType type, bool enableRangeProcessing, bool inline) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            Video entity = await db.Videos.Include(v => v.Entry).FirstOrDefaultAsync(v => v.Id == videoId);
            if (entity == null)
                return NotFound();

            if (entity.Entry?.Id != entryId)
                return BadRequest();

            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch.ToString() == videoId)
                return StatusCode(304);

            Stream content = await fileStorage.FindAsync(entry, entity, type);
            if (content == null)
                return NotFound();

            Response.Headers[HeaderNames.ETag] = videoId;

            if (type == VideoType.Thumbnail || type == VideoType.Preview)
            {
                Response.Headers[HeaderNames.CacheControl] = CacheHeaderValue;
                return File(content, "image/jpeg");
            }

            // Original video
            string contentType = entity.ContentType;
            if (string.IsNullOrEmpty(contentType))
                contentType = GetFileContentType(entity.FileName);

            // Let ASP.NET Core handle Range only if the stream is seekable.
            bool canRangeProcess = enableRangeProcessing && content.CanSeek;
            return File(content, contentType, enableRangeProcessing: canRangeProcess);
        });

        private static string GetFileContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string contentType))
                contentType = "application/octet-stream";

            return contentType;
        }

        [HttpPut("{videoId}")]
        public Task<IActionResult> Update(string entryId, string videoId, VideoModel model) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            Video entity = await db.Videos.AsTracking().FirstOrDefaultAsync(v => v.Entry.Id == entryId && v.Id == videoId);
            if (entity == null)
                return NotFound();

            service.MapModelToEntity(model, entity);
            await db.SaveChangesAsync();
            return NoContent();
        });

        [HttpDelete("{videoId}")]
        public Task<IActionResult> Delete(string entryId, string videoId) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            Video entity = await db.Videos.FirstOrDefaultAsync(v => v.Entry.Id == entryId && v.Id == videoId);
            if (entity == null)
                return NotFound();

            await service.DeleteAsync(entry, entity);

            return Ok();
        });

        [HttpPost("{videoId}/set-location-from-original")]
        public Task<IActionResult> SetLocationFromOriginal(string entryId, string videoId) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            Video entity = await db.Videos.FirstOrDefaultAsync(v => v.Entry.Id == entryId && v.Id == videoId);
            if (entity == null)
                return NotFound();

            await service.SetLocationFromOriginalAsync(entry, entity);

            return NoContent();
        });
    }
}
