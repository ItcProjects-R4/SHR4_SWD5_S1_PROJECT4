using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface ISystemSettingService
    {
        Task<string> GetSettingValueAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default);
        Task<T> GetSettingValueAsync<T>(string key, T defaultValue = default!, CancellationToken cancellationToken = default);
        Task SaveSettingAsync(string key, string value, CancellationToken cancellationToken = default);
        Task SaveSettingsAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);
        Task<bool> CanConnectDatabaseAsync(CancellationToken cancellationToken = default);
    }
}

