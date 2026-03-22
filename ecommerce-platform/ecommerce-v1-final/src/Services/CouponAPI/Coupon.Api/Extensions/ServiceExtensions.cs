using Common.Application.Behaviors;
using Coupon.Application.Commands;
using Coupon.Application.Interfaces;
using Coupon.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Coupon.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCouponServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<CouponDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("CouponDb"), npg => npg.EnableRetryOnFailure(3)));
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreateCouponCommand>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<CreateCouponValidator>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IUnitOfWorkCoupon, UnitOfWorkCoupon>();
        var jwt = config.GetSection("JwtSettings");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt["Issuer"],
                ValidateAudience = true, ValidAudience = jwt["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            });
        services.AddAuthorization(opts => opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));
        services.AddApiVersioning(o => { o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Coupon API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer", In = ParameterLocation.Header });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        });
        services.AddHealthChecks().AddDbContextCheck<CouponDbContext>();
        return services;
    }
}
