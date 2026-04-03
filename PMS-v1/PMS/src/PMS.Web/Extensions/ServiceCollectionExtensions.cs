using PMS.Application.Interfaces.Services;
using PMS.Application.Services;

namespace PMS.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Web-layer services cleanly.
    /// </summary>
    public static IServiceCollection AddWebServices(
        this IServiceCollection services)
    {
        // Memory cache (backing store for CacheService)
        services.AddMemoryCache(opts =>
        {
            opts.SizeLimit = null; // unlimited (monitor in prod)
            opts.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        // Cache service — Singleton because IMemoryCache is Singleton
        services.AddSingleton<ICacheService, CacheService>();

        // Response compression
        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
        });

        return services;
    }
}