using AdminPortal.Application.Interfaces;
using AdminPortal.Application.Services;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.Repositories;
using AdminPortal.Infrastructure.Security;
using AdminPortal.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Store repositories
        services.AddSingleton<IStoreRepository, MockStoreRepository>();
        services.AddSingleton<IProductRepository, MockProductRepository>();
        services.AddSingleton<IOrderRepository, MockOrderRepository>();
        services.AddSingleton<IAnalyticsRepository, MockAnalyticsRepository>();
        services.AddSingleton<IDiscountRepository, MockDiscountRepository>();
        services.AddSingleton<IStaffRepository, MockStaffRepository>();
        services.AddSingleton<IPayoutRepository, MockPayoutRepository>();
        services.AddSingleton<ICustomerRepository, MockCustomerRepository>();
        services.AddSingleton<ICreditRepository, MockCreditRepository>();

        // Auth repositories
        services.AddSingleton<IUserRepository, MockUserRepository>();
        services.AddSingleton<IPasswordResetTokenRepository, MockPasswordResetTokenRepository>();

        // Security services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailService, MockEmailService>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICreditService, CreditService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
