using Microsoft.Extensions.DependencyInjection;
using STHEnterprise.Application.Interfaces;
using STHEnterprise.Infrastructure.Services;

namespace STHEnterprise.Infrastructure
{

    /*
     *  | Method | Endpoint              | Description   |
        | ------ | --------------------- | ------------- |
        | GET    | `/api/v1/orders`      | My Orders     |
        | GET    | `/api/v1/orders/{id}` | Order Details |        
     */
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICheckoutService, CheckoutService>();
            services.AddScoped<IOrderService, OrderService>();

            return services;
        }
    }
}
