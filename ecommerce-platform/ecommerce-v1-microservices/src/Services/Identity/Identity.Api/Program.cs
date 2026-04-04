using Asp.Versioning;
using Common.Api.Middleware;
using Common.Application.Behaviors;
using FluentValidation;
using Identity.Application.Commands;
using Identity.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("IdentityDb"),
        npg => npg.EnableRetryOnFailure(3)));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RegisterCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<ITokenService,       JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher,     BCryptPasswordHasher>();
builder.Services.AddScoped<IUserRepository,     UserRepository>();
builder.Services.AddScoped<IUnitOfWorkIdentity, UnitOfWorkIdentity>();

var jwtCfg = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,  ValidIssuer   = jwtCfg["Issuer"],
        ValidateAudience         = true,  ValidAudience = jwtCfg["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(jwtCfg["SecretKey"]!)),
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    });
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly",     p => p.RequireRole("Admin"));
    opts.AddPolicy("VendorOrAdmin", p => p.RequireRole("Vendor", "Admin"));
});

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat      = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Identity API",
        Version     = "v1",
        Description = "POST /api/v1/demo/complete-flow to run the full Identity workflow test."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("identity-db");

var app = builder.Build();

// ── Create tables via raw SQL (works even when DB already exists but is empty) ──
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();

        // Create Users table if it does not exist
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id""                   UUID            NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                ""Email""                VARCHAR(256)    NOT NULL,
                ""FirstName""            VARCHAR(100)    NOT NULL,
                ""LastName""             VARCHAR(100)    NOT NULL,
                ""PhoneNumber""          VARCHAR(25),
                ""PasswordHash""         TEXT            NOT NULL,
                ""Role""                 VARCHAR(50)     NOT NULL DEFAULT 'Customer',
                ""EmailConfirmed""       BOOLEAN         NOT NULL DEFAULT FALSE,
                ""IsActive""             BOOLEAN         NOT NULL DEFAULT TRUE,
                ""IsDeleted""            BOOLEAN         NOT NULL DEFAULT FALSE,
                ""FailedLoginAttempts""  INTEGER         NOT NULL DEFAULT 0,
                ""LockoutEnd""           TIMESTAMPTZ,
                ""RefreshToken""         TEXT,
                ""RefreshTokenExpiry""   TIMESTAMPTZ,
                ""CreatedAt""            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                ""UpdatedAt""            TIMESTAMPTZ,
                ""CreatedBy""            TEXT,
                ""UpdatedBy""            TEXT,
                ""Version""              INTEGER         NOT NULL DEFAULT 1
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
        ");

        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Identity database tables ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Failed to initialise Identity database. " +
            "Ensure PostgreSQL is running: docker compose up -d postgres redis");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
