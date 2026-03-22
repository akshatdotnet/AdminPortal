using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Payment.Application.Commands;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Gateways;
using Payment.Infrastructure.Persistence;
using System.Text;

namespace Payment.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PaymentDbContext>(opts =>
            opts.UseNpgsql(
                config.GetConnectionString("PaymentDb"),
                npg => npg.EnableRetryOnFailure(3)));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreatePaymentSessionCommand>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<CreatePaymentSessionValidator>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWorkPayment, UnitOfWorkPayment>();
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        services.AddHttpClient<IOrderServiceClient, HttpOrderServiceClient>(c =>
            c.BaseAddress = new Uri(
                config["ServiceUrls:OrderApi"] ?? "http://localhost:5003"));

        var jwt = config.GetSection("JwtSettings");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts => opts.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,          ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,         ValidAudience = jwt["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                });

        services.AddAuthorization(opts =>
            opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http,
                Scheme = "Bearer", In = ParameterLocation.Header
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
        services.AddHealthChecks().AddDbContextCheck<PaymentDbContext>();
        return services;
    }
}
