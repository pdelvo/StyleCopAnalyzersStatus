using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using StyleCopAnalyzers.Status.Common;

namespace StyleCopAnalyzers.Status.Website.Models
{
    public class WebDataResolver : IDataResolver
    {
        IHostingEnvironment hostingEnvironment;
        IMemoryCache memoryCache;
        StatusPageOptions options;

        public WebDataResolver(
            IHostingEnvironment hostingEnvironment,
            IMemoryCache memoryCache,
            IOptions<StatusPageOptions> options)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.memoryCache = memoryCache;
            this.options = options.Value;
        }

        public async Task<MainViewModel> ResolveAsync(string sha1)
        {
            if (sha1 != null && sha1.Contains('.'))
            {
                throw new ArgumentOutOfRangeException(nameof(sha1));
            }

            string branch = sha1 ?? this.options.DefaultBranch;

            MainViewModel model;
            if (memoryCache.TryGetValue(branch, out model))
            {
                return model;
            }

            Uri path = new Uri(new Uri(this.options.DataUri), branch + ".json");

            HttpClient httpClient = new HttpClient();
            string json = await httpClient.GetStringAsync(path);

            var mainViewModel = new MainViewModel
            {
                Diagnostics = JsonConvert.DeserializeObject<IEnumerable<StyleCopDiagnostic>>(json),
                CommitId = sha1
            };

            return memoryCache.Set<MainViewModel>(branch, mainViewModel, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) });
        }
    }
}
