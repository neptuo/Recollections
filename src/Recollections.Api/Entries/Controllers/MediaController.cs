using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
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
        private readonly EntryMediaMapper mediaMapper;
        private readonly IUserNameProvider userNames;
        private readonly FreeLimitsChecker freeLimits;
        private readonly StorageOptions storageOptions;

        public MediaController(DataContext db, ImageService imageService, VideoService videoService, EntryMediaMapper mediaMapper, IOptions<StorageOptions> storageOptions, ShareStatusService shareStatus, IUserNameProvider userNames, FreeLimitsChecker freeLimits)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(imageService, "imageService");
            Ensure.NotNull(videoService, "videoService");
            Ensure.NotNull(mediaMapper, "mediaMapper");
            Ensure.NotNull(storageOptions, "storageOptions");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(freeLimits, "freeLimits");
            this.db = db;
            this.imageService = imageService;
            this.videoService = videoService;
            this.mediaMapper = mediaMapper;
            this.storageOptions = storageOptions.Value;
            this.userNames = userNames;
            this.freeLimits = freeLimits;
        }

        [HttpGet]
        public Task<IActionResult> List(string entryId) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            var result = await mediaMapper.MapAsync(entry.Id, entry.UserId);
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
