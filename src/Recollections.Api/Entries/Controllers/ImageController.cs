using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoFile = System.IO.File;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/images")]
    public class ImageController : ControllerBase
    {
        private readonly DataContext dataContext;
        private readonly StorageOptions configuration;
        private readonly PathResolver pathResolver;

        public ImageController(DataContext dataContext, PathResolver pathResolver, IOptions<StorageOptions> configuration)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(pathResolver, "pathResolver");
            Ensure.NotNull(configuration, "configuration");
            this.dataContext = dataContext;
            this.pathResolver = pathResolver;
            this.configuration = configuration.Value;
        }

        private async Task<IActionResult> Run(string entryId, Func<Entry, Task<IActionResult>> handler)
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

        [HttpPost]
        public Task<IActionResult> Create(string entryId, IFormFile file) => Run(entryId, async entry =>
        {
            string imageId = Guid.NewGuid().ToString();
            string imageName = imageId + Path.GetExtension(file.FileName);

            string storagePath = GetStoragePath(entry);
            string path = Path.Combine(storagePath, imageName);

            await CopyFileAsync(file, path);

            return base.Ok();
        });

        private static async Task CopyFileAsync(IFormFile file, string path)
        {
            using (FileStream target = IoFile.Create(path))
            using (Stream source = file.OpenReadStream())
                await source.CopyToAsync(target);
        }

        private string GetStoragePath(Entry entry)
        {
            string storagePath = pathResolver(configuration.GetPath(entry.UserId, entry.Id));
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }
    }
}
