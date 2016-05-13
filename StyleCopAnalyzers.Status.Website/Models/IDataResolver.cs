using System.Threading.Tasks;

namespace StyleCopAnalyzers.Status.Website.Models
{
    public interface IDataResolver
    {
        Task<MainViewModel> ResolveAsync(string sha1);
    }
}
