using Microsoft.AspNetCore.Http;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    public class FormFileInput : IFileInput
    {
        private readonly IFormFile file;

        public FormFileInput(IFormFile file)
        {
            Ensure.NotNull(file, "file");
            this.file = file;
        }

        public string ContentType => file.ContentType;

        public string FileName => file.FileName;

        public long Length => file.Length;

        public Stream OpenReadStream() => file.OpenReadStream();
    }
}
