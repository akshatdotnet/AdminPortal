using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PMS.Infrastructure.Dapper;

namespace PMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs nightly to hard-delete soft-deleted records older than the
/// retention period, keeping the database lean.
/// </summary>
public class CleanupBackgroundService : BackgroundService
{
    private const int RetentionDays = 30;   // keep soft-deletes for 30 days
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupBackgroundService started.");

        // Wait for app to fully start before first run
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("Running nightly database cleanup...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dapper = scope.ServiceProvider
                                        .GetRequiredService<DapperContext>();

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            using var connection = dapper.CreateConnection();

            // Hard delete old soft-deleted records (leaf tables first)
            var logRows = await connection.ExecuteAsync(
                """
                DELETE FROM TaskTimeLogs
                WHERE IsDeleted = 1 AND DeletedAt < @Cutoff
                """,
                new { Cutoff = cutoff });

            var taskRows = await connection.ExecuteAsync(
                """
                DELETE FROM Tasks
                WHERE IsDeleted = 1 AND DeletedAt < @Cutoff
                  AND Id NOT IN (
                      SELECT TaskId FROM TaskTimeLogs
                      WHERE IsDeleted = 0)
                """,
                new { Cutoff = cutoff });

            var projectRows = await connection.ExecuteAsync(
                """
                DELETE FROM Projects
                WHERE IsDeleted = 1 AND DeletedAt < @Cutoff
                  AND Id NOT IN (
                      SELECT ProjectId FROM Tasks
                      WHERE IsDeleted = 0)
                """,
                new { Cutoff = cutoff });

            _logger.LogInformation(
                "Cleanup complete. Removed: {LogRows} logs, " +
                "{TaskRows} tasks, {ProjectRows} projects.",
                logRows, taskRows, projectRows);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cleanup cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during nightly cleanup.");
        }
    }
}