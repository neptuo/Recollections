using Microsoft.EntityFrameworkCore;
using Neptuo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public VideoService(DataContext dataContext, IFileStorage fileStorage, ImageResizeService resizeService, IVideoValidator validator)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(fileStorage, "fileStorage");
            Ensure.NotNull(resizeService, "resizeService");
            Ensure.NotNull(validator, "validator");
            this.dataContext = dataContext;
            this.fileStorage = fileStorage;
            this.resizeService = resizeService;
            this.validator = validator;
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
            // TODO: Explore better options than using external tool.

            

            // We rely on 'ffmpeg' being available on the server PATH.
            // Output a single frame as PNG to stdout, then convert/resize to JPEG thumbnail.

            await using Stream original = await fileStorage.FindAsync(entry, video, VideoType.Original);
            if (original == null)
                throw Ensure.Exception.InvalidOperation("Missing video content.");

            using var pngFrame = new MemoryStream();

            // ffmpeg -hide_banner -loglevel error -i pipe:0 -frames:v 1 -vf thumbnail,scale=-1:150 -f image2pipe -vcodec png pipe:1
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            startInfo.ArgumentList.Add("-hide_banner");
            startInfo.ArgumentList.Add("-loglevel");
            startInfo.ArgumentList.Add("error");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add("pipe:0");
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-vf");
            startInfo.ArgumentList.Add($"thumbnail,scale=-1:{ThumbnailHeight}");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("image2pipe");
            startInfo.ArgumentList.Add("-vcodec");
            startInfo.ArgumentList.Add("png");
            startInfo.ArgumentList.Add("pipe:1");

            using var process = Process.Start(startInfo);
            if (process == null)
                throw Ensure.Exception.InvalidOperation("Unable to start ffmpeg process.");

            var copyInputTask = Task.Run(async () =>
            {
                await original.CopyToAsync(process.StandardInput.BaseStream);
                process.StandardInput.Close();
            });

            var copyOutputTask = process.StandardOutput.BaseStream.CopyToAsync(pngFrame);
            var stderrTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(copyInputTask, copyOutputTask);
            await process.WaitForExitAsync();

            string stderr = await stderrTask;
            if (process.ExitCode != 0)
                throw new VideoUploadValidationException($"ffmpeg failed: {stderr}");

            pngFrame.Position = 0;

            // Convert to JPEG thumbnail with exact bounds
            resizeService.Thumbnail(pngFrame, outputJpeg, ThumbnailWidth, ThumbnailHeight);
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
