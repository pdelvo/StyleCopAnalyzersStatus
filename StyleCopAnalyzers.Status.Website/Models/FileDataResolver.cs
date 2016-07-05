using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StyleCopAnalyzers.Status.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StyleCopAnalyzers.Status.Website.Models
{
    public class FileDataResolver : IDataResolver
    {
        private IHostingEnvironment hostingEnvironment;
        private IMemoryCache memoryCache;
        private StatusPageOptions options;

        public FileDataResolver(
            IHostingEnvironment hostingEnvironment,
            IMemoryCache memoryCache,
            IOptions<StatusPageOptions> options)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.memoryCache = memoryCache;
            this.options = options.Value;
        }

        public Task<MainViewModel> ResolveAsync(string sha1)
        {
            if (sha1 != null && sha1.Contains('.'))
            {
                throw new ArgumentOutOfRangeException(nameof(sha1));
            }

            string branch = sha1 ?? this.options.DefaultBranch;

            MainViewModel model;
            if (memoryCache.TryGetValue(branch, out model))
            {
                return Task.FromResult(model);
            }

            string path = Path.Combine(this.options.DataDirectory, branch + ".json");
            var fileInfo = hostingEnvironment.ContentRootFileProvider.GetFileInfo(path);
            string json = File.ReadAllText(fileInfo.PhysicalPath);

            var mainViewModel = new MainViewModel
            {
                Diagnostics = JsonConvert.DeserializeObject<IEnumerable<StyleCopDiagnostic>>(json),
                CommitId = sha1
            };

            return Task.FromResult(memoryCache.Set(branch, mainViewModel, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) }));
        }
    }
}
