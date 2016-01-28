﻿using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Hosting;
using Newtonsoft.Json;
using StyleCopAnalyzers.Status.Website.Models;
using StyleCopAnalyzers.Status.Common;
using Microsoft.Extensions.OptionsModel;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                return this.HttpBadRequest();
            }

            try
            {
                MainViewModel viewModel = await this.dataResolver.ResolveAsync(sha1);

                var diagnostics = (from x in viewModel.Diagnostics
                                   where category == null || x.Category == category
                                   where hasImplementation == null || x.HasImplementation == hasImplementation
                                   where status == null || x.Status == status
                                   where codeFixStatus == null || x.CodeFixStatus == codeFixStatus
                                   where fixAllStatus == null || x.FixAllStatus == fixAllStatus
                                   select x).ToArray();

                if (diagnostics.Length == 0)
                {
                    // No entries found
                    return this.HttpNotFound();
                }

                ViewBag.Diagnostics = diagnostics;

                return this.View(viewModel);
            }
            catch (IOException)
            {
                return HttpNotFound();
            }
        }
    }
}
