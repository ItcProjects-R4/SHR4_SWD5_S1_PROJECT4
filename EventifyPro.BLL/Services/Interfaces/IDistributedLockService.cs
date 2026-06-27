using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IDistributedLockService
    {
        Task<bool> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
        Task ReleaseLockAsync(string key);
    }
}
