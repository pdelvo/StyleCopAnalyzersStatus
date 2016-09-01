using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StyleCopAnalyzers.Status.Common;
using StyleCopAnalyzers.Status.Website.Models;

namespace StyleCopAnalyzers.Status.Website.Controllers
{
    public class HomeController : Controller
    {
        IDataResolver dataResolver;

        public HomeController(IDataResolver dataResolver)
        {
            this.dataResolver = dataResolver;
        }

        public async Task<IActionResult> Index(
            string category = null,
            bool? hasImplementation = null,
            string status = null,
            CodeFixStatus? codeFixStatus = null,
            FixAllStatus? fixAllStatus = null,
            string sha1 = null)
        {
            if (sha1 != null && sha1.Contains('.'))
            {
                return this.BadRequest();
            }

            try
            {
                MainViewModel viewModel = await this.dataResolver.ResolveAsync(sha1);

                var diagnostics = viewModel.Diagnostics;

                if (!diagnostics.Any())
                {
                    // No entries found
                    return this.NotFound();
                }

                ViewBag.Diagnostics = diagnostics;
                return this.View(viewModel);
                return Content("foo");
            }
            catch (IOException)
            {
                return NotFound();
            }
        }
    }
}
