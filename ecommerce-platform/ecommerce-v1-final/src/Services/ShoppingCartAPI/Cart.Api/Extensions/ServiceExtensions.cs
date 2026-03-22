using Cart.Application.Commands;
using Cart.Application.Interfaces;
using Cart.Infrastructure.Repositories;
using Cart.Infrastructure.Services;
using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Cart.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCartServices(this IServiceCollection services, IConfiguration config)
    {
        // Redis
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = config.GetConnectionString("Redis") ?? "localhost:6379");

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<AddToCartCommand>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<AddToCartValidator>();
        services.AddScoped<ICartRepository, RedisCartRepository>();
        services.AddHttpClient<ICouponServiceClient, HttpCouponClient>(c =>
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
        services.AddAuthorization();
        services.AddApiVersioning(o => { o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cart API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer", In = ParameterLocation.Header });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        });
        services.AddHealthChecks();
        return services;
    }
}
