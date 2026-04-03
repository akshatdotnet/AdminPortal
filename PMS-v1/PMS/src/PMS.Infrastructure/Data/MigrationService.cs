using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PMS.Infrastructure.Data;

/// <summary>
/// Hosted service that applies pending migrations on startup.
/// Safe to run in production — skips if already up to date.
/// </summary>
public class MigrationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<MigrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for pending database migrations...");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
                      .GetRequiredService<ApplicationDbContext>();

        var pending = await db.Database
            .GetPendingMigrationsAsync(cancellationToken);

        var pendingList = pending.ToList();

        if (pendingList.Count == 0)
        {
            _logger.LogInformation("No pending migrations found.");
            return;
        }

        _logger.LogInformation(
            "Applying {Count} pending migration(s): {Migrations}",
            pendingList.Count,
            string.Join(", ", pendingList));

        await db.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Migrations applied successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}