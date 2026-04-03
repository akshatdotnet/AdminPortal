using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zovo.Core.Interfaces;
using Zovo.Infrastructure.Data;

namespace Zovo.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config, bool useInMemory = true)
    {
        if (useInMemory)
            services.AddDbContext<ZovoDbContext>(o => o.UseInMemoryDatabase("ZovoDB"));
        else
            services.AddDbContext<ZovoDbContext>(o =>
                o.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly("Zovo.Infrastructure")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
