using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neptuo;
using Neptuo.Recollections.Sharing;
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

        public VideoController(DataContext db, IFileStorage fileStorage, ShareStatusService shareStatus)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(fileStorage, "fileStorage");
            this.db = db;
            this.fileStorage = fileStorage;
        }

        [HttpGet("{videoId}/thumbnail")]
        public Task<IActionResult> Thumbnail(string entryId, string videoId)
            => GetFileContent(entryId, videoId, VideoType.Thumbnail, enableRangeProcessing: false, inline: true);

        [HttpGet("{videoId}/original")]
        public Task<IActionResult> Original(string entryId, string videoId)
            => GetFileContent(entryId, videoId, VideoType.Original, enableRangeProcessing: true, inline: true);

        private Task<IActionResult> GetFileContent(string entryId, string videoId, VideoType type, bool enableRangeProcessing, bool inline)
            => RunEntryAsync(entryId, Permission.Read, async entry =>
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

                if (type == VideoType.Thumbnail)
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
    }
}
