namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a system configuration setting.
/// </summary>
public class SystemSetting : AuditableEntity
{
    /// <summary>
    /// Gets or sets the setting key (e.g. PlatformName, TicketCommissionRate).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
