namespace EventifyPro.BLL.Services.Implementations
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly EventifyDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "SystemSetting_";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

        public SystemSettingService(EventifyDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string> GetSettingValueAsync(string key, string defaultValue = "", CancellationToken cancellationToken = default)
        {
            var cacheKey = CachePrefix + key;
            if (_cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            var value = setting?.Value ?? defaultValue;

            _cache.Set(cacheKey, value, CacheExpiry);

            return value;
        }

        public async Task<T> GetSettingValueAsync<T>(string key, T defaultValue = default!, CancellationToken cancellationToken = default)
        {
            var stringValue = await GetSettingValueAsync(key, string.Empty, cancellationToken);
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return defaultValue;
            }

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(stringValue, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task SaveSettingAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = CachePrefix + key;
            _cache.Remove(cacheKey);
        }

        public async Task SaveSettingsAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default)
        {
            if (settings == null || settings.Count == 0) return;

            var keys = settings.Keys.ToList();
            var existingSettings = await _context.SystemSettings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync(cancellationToken);

            foreach (var kvp in settings)
            {
                var setting = existingSettings.FirstOrDefault(s => s.Key == kvp.Key);
                if (setting == null)
                {
                    setting = new SystemSetting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.SystemSettings.Add(setting);
                }
                else
                {
                    setting.Value = kvp.Value ?? string.Empty;
                    setting.UpdatedAt = DateTime.UtcNow;
                    _context.SystemSettings.Update(setting);
                }

                // Invalidate cache
                var cacheKey = CachePrefix + kvp.Key;
                _cache.Remove(cacheKey);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> CanConnectDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.CanConnectAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }
    }
}

