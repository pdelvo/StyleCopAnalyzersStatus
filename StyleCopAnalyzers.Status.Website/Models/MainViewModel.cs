using StyleCopAnalyzers.Status.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StyleCopAnalyzers.Status.Website.Models
{
    public class MainViewModel
    {
        public string CommitId { get; set; }

        public IEnumerable<StyleCopDiagnostic> Diagnostics { get; set; }
    }
}
