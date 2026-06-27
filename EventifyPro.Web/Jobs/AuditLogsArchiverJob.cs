namespace EventifyPro.Web.Jobs;

/// <summary>
/// Hangfire job that periodically archives database audit logs older than 6 months
/// to keep the active AuditLogs table small and fast.
/// </summary>
public class AuditLogsArchiverJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogsArchiverJob> _logger;

    public AuditLogsArchiverJob(
        IServiceProvider serviceProvider,
        ILogger<AuditLogsArchiverJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the archiving process
    /// </summary>
    public async Task ArchiveAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Hangfire audit log archiving job...");
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventifyDbContext>();

        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        int totalArchived = 0;
        const int batchSize = 500;

        while (true)
        {
            // Fetch a batch of old logs (tracked to support deletion)
            var oldLogs = await context.AuditLogs
                .Where(log => log.ChangedAt < cutoffDate)
                .OrderBy(log => log.ChangedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (oldLogs.Count == 0)
            {
                break;
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var archivedLogs = oldLogs.Select(log => new ArchivedAuditLog
                {
                    TableName = log.TableName,
                    Action = log.Action,
                    EntityId = log.EntityId,
                    OldValues = log.OldValues,
                    NewValues = log.NewValues,
                    UserId = log.UserId,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    ChangedAt = log.ChangedAt,
                    ArchivedAt = DateTime.UtcNow
                }).ToList();

                // Add to archive table
                await context.ArchivedAuditLogs.AddRangeAsync(archivedLogs, cancellationToken);

                // Remove from active logs table
                context.AuditLogs.RemoveRange(oldLogs);

                // Save changes
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                totalArchived += oldLogs.Count;
                _logger.LogInformation("Archived batch of {Count} audit logs.", oldLogs.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while executing audit log archiving batch.");
                throw;
            }
        }

        if (totalArchived > 0)
        {
            _logger.LogInformation("Successfully archived {Count} old audit logs.", totalArchived);
        }
        else
        {
            _logger.LogInformation("No audit logs older than 6 months found to archive.");
        }
    }
}
