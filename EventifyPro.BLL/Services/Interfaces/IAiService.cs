using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces;

public interface IAiService
{
    Task<string> GenerateEventDescriptionAsync(string title, string city, string category, CancellationToken cancellationToken = default);
}
