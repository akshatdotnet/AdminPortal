using ECommerce.Application;
using ECommerce.Console;
using ECommerce.Console.Extensions;
using ECommerce.Console.Handlers;
using ECommerce.Infrastructure.Extensions;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ─────────────────────────────────────────────────────────────────────────────
//  ECommerce .NET 8 — Clean Architecture Console App
//  USAGE:
//    dotnet run                   → interactive menu
//    dotnet run -- --demo         → automated full-flow demo (no input needed)
//    dotnet run -- --demo --reset → wipe DB + fresh seed + run demo
//    dotnet run -- --reset        → wipe and re-seed only
// ─────────────────────────────────────────────────────────────────────────────

bool isDemoMode = args.Contains("--demo");
bool isReset    = args.Contains("--reset");

var cts = new CancellationTokenSource();
System.Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System",    LogLevel.Warning);
        logging.AddFilter("ECommerce", isDemoMode ? LogLevel.Warning : LogLevel.Information);
        logging.AddConsole(opt => opt.FormatterName = "simple");
    })
    .ConfigureServices((ctx, services) =>
    {
        services
            .AddApplication()
            .AddInfrastructure(ctx.Configuration)
            .AddConsoleHandlers();
    })
    .Build();

// ── One-time startup: DB creation + seeding in its own scope ─────────────────
using (var startupScope = host.Services.CreateScope())
{
    var db     = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seeder = startupScope.ServiceProvider.GetRequiredService<DataSeeder>();

    if (isReset)
    {
        await db.Database.EnsureDeletedAsync(cts.Token);
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine("  [RESET] Database wiped and will be re-seeded.");
        System.Console.ResetColor();
    }

    await db.Database.EnsureCreatedAsync(cts.Token);
    await seeder.SeedAsync();
}

// ── Route: demo or interactive ────────────────────────────────────────────────
if (isDemoMode)
{
    // Each operation in the demo creates its own child scope (fresh DbContext).
    // We only need the scope factory here — not a long-lived scope.
    var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

    // Load customer list in a short-lived scope
    string customerName;
    Guid   customerId;
    using (var scope = scopeFactory.CreateScope())
    {
        var db        = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customers = await db.Customers.ToListAsync(cts.Token);
        if (!customers.Any())
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("  No customers found. Run with --reset to re-seed.");
            System.Console.ResetColor();
            return;
        }
        customerId   = customers.First().Id;
        customerName = customers.First().FullName;
    }

    System.Console.ForegroundColor = ConsoleColor.DarkYellow;
    System.Console.WriteLine($"\n  [DEMO] Running as: {customerName}");
    System.Console.ResetColor();

    // DemoFlowRunner receives the scope factory; it creates a fresh scope per operation
    using var demoScope = scopeFactory.CreateScope();
    var runner = demoScope.ServiceProvider.GetRequiredService<DemoFlowRunner>();
    await runner.RunFullDemoAsync(customerId, customerName, cts.Token);
}
else
{
    // Interactive: EcommerceApp creates a child scope per menu selection
    var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
    using var appScope = scopeFactory.CreateScope();
    var app = appScope.ServiceProvider.GetRequiredService<EcommerceApp>();
    await app.RunAsync(cts.Token);
}
