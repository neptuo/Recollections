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
using IsImage = SixLabors.ImageSharp.Image;

namespace Neptuo.Recollections.Entries
{
    public class VideoService
    {
        private const int PreviewWidth = 1024;
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

            using var imageContent = await ExtractImageFromVideoAsync(entry, entity);
            
            ImageInfo imageInfo = IsImage.Identify(imageContent);
            entity.OriginalWidth = imageInfo.Width;
            entity.OriginalHeight = imageInfo.Height;

            imageContent.Position = 0;
            using (var thumbnailContent = new MemoryStream())
            {
                resizeService.Thumbnail(imageContent, thumbnailContent, ThumbnailWidth, ThumbnailHeight);
                thumbnailContent.Position = 0;

                await fileStorage.SaveAsync(entry, entity, thumbnailContent, VideoType.Thumbnail);
            }
            
            imageContent.Position = 0;
            using (var previewContent = new MemoryStream())
            {
                resizeService.Resize(imageContent, previewContent, PreviewWidth);
                previewContent.Position = 0;
                
                await fileStorage.SaveAsync(entry, entity, previewContent, VideoType.Preview);
            }

            await dataContext.SaveChangesAsync();
            return entity;
        }

        private async Task<Stream> ExtractImageFromVideoAsync(Entry entry, Video video)
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
                using var videoImage = IsImage.Load(decoderOptions, original);
                if (videoImage.Frames.Count == 0)
                    throw new VideoUploadValidationException("Video contains no decodable frames.");

                var outputStream = new MemoryStream();
                videoImage.Save(outputStream, formatDefinition.Codec);
                outputStream.Position = 0;
                
                return outputStream;
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
            await fileStorage.DeleteAsync(entry, entity, VideoType.Preview);
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

            var previewSize = resizeService.GetResizedBounds(entity.OriginalWidth, entity.OriginalHeight, PreviewWidth);
            model.Original = new MediaSourceModel($"{basePath}/original", entity.OriginalWidth, entity.OriginalHeight);
            model.Preview = new MediaSourceModel($"{basePath}/preview", previewSize.width, previewSize.height);
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
