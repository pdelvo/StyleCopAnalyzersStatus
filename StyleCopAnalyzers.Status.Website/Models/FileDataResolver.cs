using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.OptionsModel;
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
        IHostingEnvironment hostingEnvironment;
        IMemoryCache memoryCache;
        StatusPageOptions options;

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

            string json = System.IO.File.ReadAllText(hostingEnvironment.MapPath(path));

            var mainViewModel = new MainViewModel
            {
                Diagnostics = JsonConvert.DeserializeObject<IEnumerable<StyleCopDiagnostic>>(json),
                CommitId = sha1
            };

            return Task.FromResult(memoryCache.Set<MainViewModel>(branch, mainViewModel, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) }));
        }
    }
}
