using Microsoft.Extensions.DependencyInjection;
using Zovo.Application.Customers;
using Zovo.Application.Dashboard;
using Zovo.Application.Orders;
using Zovo.Application.Products;

namespace Zovo.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService,  ProductService>();
        services.AddScoped<IOrderService,    OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
