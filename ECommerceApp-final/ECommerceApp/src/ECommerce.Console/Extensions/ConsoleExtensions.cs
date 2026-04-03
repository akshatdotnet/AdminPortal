using ECommerce.Console.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Console.Extensions;

public static class ConsoleExtensions
{
    public static IServiceCollection AddConsoleHandlers(this IServiceCollection services)
    {
        services.AddScoped<ProductHandler>();
        services.AddScoped<CartHandler>();
        services.AddScoped<OrderHandler>();
        services.AddScoped<PaymentHandler>();
        services.AddScoped<CustomerHandler>();
        services.AddScoped<DemoFlowRunner>();
        services.AddScoped<EcommerceApp>();
        return services;
    }
}
