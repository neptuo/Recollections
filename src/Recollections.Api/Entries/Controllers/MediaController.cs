using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/media")]
    public class MediaController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ImageService imageService;
        private readonly VideoService videoService;
        private readonly IUserNameProvider userNames;
        private readonly FreeLimitsChecker freeLimits;
        private readonly StorageOptions storageOptions;

        public MediaController(DataContext db, ImageService imageService, VideoService videoService, IOptions<StorageOptions> storageOptions, ShareStatusService shareStatus, IUserNameProvider userNames, FreeLimitsChecker freeLimits)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(imageService, "imageService");
            Ensure.NotNull(videoService, "videoService");
            Ensure.NotNull(storageOptions, "storageOptions");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.db = db;
            this.imageService = imageService;
            this.videoService = videoService;
            this.storageOptions = storageOptions.Value;
            this.userNames = userNames;
            this.freeLimits = freeLimits;
        }

        [HttpGet]
        public Task<IActionResult> List(string entryId) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            List<Image> images = await db.Images
                .Where(i => i.Entry.Id == entryId)
                .OrderBy(i => i.When)
                .ToListAsync();

            List<Video> videos = await db.Videos
                .Where(v => v.Entry.Id == entryId)
                .OrderBy(v => v.When)
                .ToListAsync();

            var result = new List<MediaModel>();
            foreach (var image in images)
            {
                var model = new ImageModel();
                imageService.MapEntityToModel(image, model, entry.UserId);
                result.Add(new MediaModel { Type = "image", Image = model });
            }

            foreach (var video in videos)
            {
                var model = new VideoModel();
                videoService.MapEntityToModel(video, model, entry.UserId);
                result.Add(new MediaModel { Type = "video", Video = model });
            }

            // Order by When across both types
            result = result.OrderBy(m => m.Type == "image" ? m.Image.When : m.Video.When).ToList();

            return Ok(result);
        });

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(200_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public Task<IActionResult> Create(string entryId, [FromForm] IFormFile file) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            if (file == null)
                return BadRequest();

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            // decide by extension
            string extension = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
            bool isVideo = extension != null && storageOptions.Videos.IsSupportedExtension(extension);
            if (isVideo)
            {
                if (!await freeLimits.CanCreateVideoAsync(userId, entryId))
                    return PremiumRequired();

                try
                {
                    Video entity = await videoService.CreateAsync(entry, new FormFileInput(file));
                    var model = new VideoModel();
                    videoService.MapEntityToModel(entity, model, entry.UserId);
                    return Ok(new MediaModel { Type = "video", Video = model });
                }
                catch (VideoUploadValidationException)
                {
                    return BadRequest();
                }
            }
            else
            {
                if (!await freeLimits.CanCreateImageAsync(userId, entryId))
                    return PremiumRequired();

                try
                {
                    Image entity = await imageService.CreateAsync(entry, new FormFileInput(file));
                    var model = new ImageModel();
                    imageService.MapEntityToModel(entity, model, entry.UserId);
                    return Ok(new MediaModel { Type = "image", Image = model });
                }
                catch (ImageUploadValidationException)
                {
                    return BadRequest();
                }
            }
        });
    }
}
