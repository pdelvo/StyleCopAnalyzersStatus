using StyleCopAnalyzers.Status.Common;
using System.Collections.Generic;

namespace StyleCopAnalyzers.Status.Website.Models
{
    public class MainViewModel
    {
        public string CommitId { get; set; }

        public IEnumerable<StyleCopDiagnostic> Diagnostics { get; set; }
    }
}
