using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Order.Application.Commands;
using Order.Application.Interfaces;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Services;
using System.Text;

namespace Order.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddOrderServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrderDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("OrderDb"), npg => npg.EnableRetryOnFailure(3)));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>();
            cfg.RegisterServicesFromAssembly(typeof(GetOrderByIdQueryHandler).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<PlaceOrderValidator>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWorkOrder, UnitOfWorkOrder>();

        // HTTP clients for inter-service calls
        services.AddHttpClient<IProductServiceClient, HttpProductServiceClient>(c =>
            c.BaseAddress = new Uri(config["ServiceUrls:ProductApi"] ?? "http://localhost:5002"));
        services.AddHttpClient<ICouponServiceClient, HttpCouponServiceClient>(c =>
            c.BaseAddress = new Uri(config["ServiceUrls:CouponApi"] ?? "http://localhost:5005"));

        var jwtSection = config.GetSection("JwtSettings");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true, ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!)),
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            });
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            opts.AddPolicy("VendorOrAdmin", p => p.RequireRole("Vendor", "Admin"));
        });

        services.AddApiVersioning(o => { o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer", In = ParameterLocation.Header });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        });
        services.AddHealthChecks().AddDbContextCheck<OrderDbContext>();
        return services;
    }
}
