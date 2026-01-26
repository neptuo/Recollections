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
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

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

            using var imageContent = new MemoryStream();
            using (var videoImage = await ExtractImageFromVideoAsync(entry, entity))
            {
                videoImage.Save(imageContent, formatDefinition.Codec);
                imageContent.Position = 0;

                entity.OriginalWidth = videoImage.Width;
                entity.OriginalHeight = videoImage.Height;
                SetProperties(entity, videoImage.Metadata);
            }

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

        private bool TryGetMetadata(ImageMetadata imageMetadata, out AVMetadata metadata)
        {
            if (imageMetadata.DecodedImageFormat is IImageFormat<AVMetadata> avFormat && imageMetadata.TryGetFormatMetadata(avFormat, out metadata))
                return metadata != null;

            metadata = null;
            return false;
        }

        private void SetProperties(Video entity, ImageMetadata imageMetadata, bool isWhenIncluded = true)
        {
            if (TryGetMetadata(imageMetadata, out var metadata))
            {
                entity.Duration = metadata.Duration.TotalSeconds;

                if (isWhenIncluded && metadata.ContainerMetadata.TryGetValue("creation_time", out var creationTimeRaw) && DateTime.TryParse(creationTimeRaw.ToString(), out var creationTime))
                {
                    entity.When = creationTime;
                }

                if (metadata.ContainerMetadata.TryGetValue("location", out var locationRaw))
                {
                    locationRaw = locationRaw.Trim('/');
                    var parts = locationRaw.Split('+', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        if (double.TryParse(parts[0], CultureInfo.InvariantCulture, out var latitude) && double.TryParse(parts[1], CultureInfo.InvariantCulture, out var longitude))
                        {
                            if (entity.Location == null)
                                entity.Location = new();

                            entity.Location.Latitude = latitude;
                            entity.Location.Longitude = longitude;

                            if (parts.Length >= 3)
                            {
                                if (double.TryParse(parts[2], CultureInfo.InvariantCulture, out var altitude))
                                    entity.Location.Altitude = altitude;
                            }
                        }
                    }
                }
            }
        }

        private async Task<IsImage> ExtractImageFromVideoAsync(Entry entry, Video video)
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
                var videoImage = IsImage.Load(decoderOptions, original);
                if (videoImage.Frames.Count == 0)
                    throw new VideoUploadValidationException("Video contains no decodable frames.");

                if (TryGetMetadata(videoImage.Metadata, out var metadata) && metadata.VideoStreams.Count > 0)
                {
                    var rotation = metadata.VideoStreams.First().Rotation;
                    if (rotation != 0)
                    {
                        switch (rotation)
                        {
                            case 90:
                                videoImage.Mutate(ctx => ctx.Rotate(RotateMode.Rotate90));
                                break;
                            case 180:
                                videoImage.Mutate(ctx => ctx.Rotate(RotateMode.Rotate180));
                                break;
                            case 270:
                                videoImage.Mutate(ctx => ctx.Rotate(RotateMode.Rotate270));
                                break;
                        }
                    }
                }

                return videoImage;
            }
            catch (Exception ex) when (ex is not VideoUploadValidationException)
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

            model.Duration = entity.Duration;
            if (entity.Location != null)
            {
                model.Location.Latitude = entity.Location.Latitude;
                model.Location.Longitude = entity.Location.Longitude;
                model.Location.Altitude = entity.Location.Altitude;
            }

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

        public void MapModelToEntity(VideoModel model, Video entity)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.When = model.When;

            if (entity.Location == null)
                entity.Location = new();

            entity.Location.Latitude = model.Location.Latitude;
            entity.Location.Longitude = model.Location.Longitude;
            entity.Location.Altitude = model.Location.Altitude;
        }

        public async Task SetLocationFromOriginalAsync(Entry entry, Video video)
        {
            var videoContent = await fileStorage.FindAsync(entry, video, VideoType.Original);
            if (videoContent == null)
                return;

            using var videoImage = await ExtractImageFromVideoAsync(entry, video);
            SetProperties(video, videoImage.Metadata, isWhenIncluded: false);

            await dataContext.SaveChangesAsync();
        }
    }
}
