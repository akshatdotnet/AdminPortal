using BookingSystem.Worker.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices(services =>
    {
        services.AddHostedService<EmailNotificationWorker>();
        services.AddHostedService<AnalyticsWorker>();
    })
    .Build();

Log.Information("Worker Service starting...");
await host.RunAsync();
