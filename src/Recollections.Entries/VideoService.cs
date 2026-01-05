using Microsoft.EntityFrameworkCore;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeyRed.ImageSharp.AVCodecFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace Neptuo.Recollections.Entries
{
    public class VideoService
    {
        private const int ThumbnailWidth = 200;
        private const int ThumbnailHeight = 150;

        private readonly DataContext dataContext;
        private readonly IFileStorage fileStorage;
        private readonly ImageResizeService resizeService;
        private readonly IVideoValidator validator;
        private readonly ImageFormatDefinition formatDefinition;

        public VideoService(DataContext dataContext, IFileStorage fileStorage, ImageResizeService resizeService, IVideoValidator validator, ImageFormatDefinition formatDefinition)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileStorage, "fileStorage");
            Ensure.NotNull(resizeService, "resizeService");
            Ensure.NotNull(validator, "validator");
            Ensure.NotNull(formatDefinition, "formatDefinition");
            this.dataContext = dataContext;
            this.fileStorage = fileStorage;
            this.resizeService = resizeService;
            this.validator = validator;
            this.formatDefinition = formatDefinition;
        }

        public async Task<Video> CreateAsync(Entry entry, IFileInput file)
        {
            Ensure.NotNull(entry, "entry");
            Ensure.NotNull(file, "file");

            await validator.ValidateAsync(entry.UserId, file);

            string videoId = Guid.NewGuid().ToString();
            var entity = new Video()
            {
                Id = videoId,
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = videoId + Path.GetExtension(file.FileName),
                ContentType = file.ContentType,
                Created = DateTime.Now,
                When = entry.When,
                Entry = entry,
            };

            await dataContext.Videos.AddAsync(entity);
            await dataContext.SaveChangesAsync();

            using (Stream source = file.OpenReadStream())
                await fileStorage.SaveAsync(entry, entity, source, VideoType.Original);

            // Extract thumbnail and store it as JPEG (same pipeline as images)
            using (var thumbnailContent = new MemoryStream())
            {
                await ExtractThumbnailAsync(entry, entity, thumbnailContent);
                thumbnailContent.Position = 0;
                await fileStorage.SaveAsync(entry, entity, thumbnailContent, VideoType.Thumbnail);

                thumbnailContent.Position = 0;
                var (width, height) = resizeService.GetSize(thumbnailContent);
                entity.OriginalWidth = width;
                entity.OriginalHeight = height;
            }

            await dataContext.SaveChangesAsync();
            return entity;
        }

        private async Task ExtractThumbnailAsync(Entry entry, Video video, Stream outputJpeg)
        {
            await using Stream original = await fileStorage.FindAsync(entry, video, VideoType.Original);
            if (original == null)
                throw Ensure.Exception.InvalidOperation("Missing video content.");

            // Decode a small number of frames from the video and use the first decoded frame.
            // Requires ImageSharp.AVCodecFormats (+ native package) to be available at runtime.
            var configuration = new Configuration().WithAVDecoders();
            var decoderOptions = new DecoderOptions
            {
                Configuration = configuration,
                MaxFrames = 1,
            };

            try
            {
                using var videoImage = SixLabors.ImageSharp.Image.Load(decoderOptions, original);
                if (videoImage.Frames.Count == 0)
                    throw new VideoUploadValidationException("Video contains no decodable frames.");

                // using var firstFrame = videoImage.Frames.CloneFrame(0);
                // using var firstFramePng = new MemoryStream();
                // firstFrame.SaveAsPng(firstFramePng);
                // firstFramePng.Position = 0;

                using var outputStream = new MemoryStream();
                videoImage.Save(outputStream, formatDefinition.Codec);
                outputStream.Position = 0;
                
                resizeService.Thumbnail(outputStream, outputJpeg, ThumbnailWidth, ThumbnailHeight);
            }
            catch (VideoUploadValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new VideoUploadValidationException("Unable to extract thumbnail from video.", ex);
            }
        }

        public async Task DeleteAsync(Entry entry, Video entity)
        {
            dataContext.Videos.Remove(entity);
            await dataContext.SaveChangesAsync();
            await fileStorage.DeleteAsync(entry, entity, VideoType.Original);
            await fileStorage.DeleteAsync(entry, entity, VideoType.Thumbnail);
        }

        public void MapEntityToModel(Video entity, VideoModel model, string userId)
        {
            model.Id = entity.Id;
            model.UserId = userId;
            model.Name = entity.Name;
            model.Description = entity.Description;
            model.When = entity.When;
            model.ContentType = entity.ContentType;

            string basePath = $"api/entries/{entity.Entry.Id}/videos/{entity.Id}";
            model.Original = new MediaSourceModel($"{basePath}/original", entity.OriginalWidth, entity.OriginalHeight);
            model.Thumbnail = new MediaSourceModel($"{basePath}/thumbnail", ThumbnailWidth, ThumbnailHeight);
        }

        public void MapEntitiesToModels(IEnumerable<Video> entities, ICollection<VideoModel> models, string userId)
        {
            foreach (var entity in entities)
            {
                var model = new VideoModel();
                MapEntityToModel(entity, model, userId);
                models.Add(model);
            }
        }
    }
}
