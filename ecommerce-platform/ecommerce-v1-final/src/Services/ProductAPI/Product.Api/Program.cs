using Asp.Versioning;
using Common.Api.Middleware;
using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Product.Application.Commands;
using Product.Application.Interfaces;
using Product.Application.Queries;
using Product.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("ProductDb"),
        npg => npg.EnableRetryOnFailure(3)));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateProductCommand>();
    cfg.RegisterServicesFromAssemblyContaining<GetProductByIdQuery>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddScoped<IProductRepository,     ProductRepository>();
builder.Services.AddScoped<ICategoryRepository,    CategoryRepository>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();
builder.Services.AddScoped<IUnitOfWorkProduct,     UnitOfWorkProduct>();

var jwt = builder.Configuration.GetSection("JwtSettings");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true, ValidIssuer   = jwt["Issuer"],
        ValidateAudience         = true, ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
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
        Title       = "Product API",
        Version     = "v1",
        Description = "POST /api/v1/demo/complete-flow — runs all 15 steps end-to-end (no auth needed)."
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
    .AddDbContextCheck<ProductDbContext>("product-db");

var app = builder.Build();

// ── Step 1: Ensure the database itself exists ─────────────────
// Connect to the maintenance DB (identity_db which always exists)
// and CREATE DATABASE product_db if it doesn't exist yet.
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProductDbContext>>();

    var productConnStr = config.GetConnectionString("ProductDb")!;

    // Build a connection string that points to the maintenance database
    // (identity_db always exists because it is POSTGRES_DB in docker-compose)
    var maintenanceConnStr = productConnStr
        .Replace("Database=product_db", "Database=identity_db",
            StringComparison.OrdinalIgnoreCase)
        .Replace("database=product_db", "Database=identity_db",
            StringComparison.OrdinalIgnoreCase);

    // If the replacement didn't work (connection string uses different format),
    // try replacing by the db name directly
    if (maintenanceConnStr == productConnStr)
    {
        var builder2 = new NpgsqlConnectionStringBuilder(productConnStr)
        {
            Database = "identity_db"
        };
        maintenanceConnStr = builder2.ConnectionString;
    }

    try
    {
        await using var mainConn = new NpgsqlConnection(maintenanceConnStr);
        await mainConn.OpenAsync();

        // Check if product_db already exists
        await using var checkCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = 'product_db'", mainConn);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            logger.LogInformation("Creating database product_db...");
            await using var createCmd = new NpgsqlCommand(
                "CREATE DATABASE product_db", mainConn);
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Database product_db created.");
        }
        else
        {
            logger.LogInformation("Database product_db already exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Could not connect to PostgreSQL to create product_db. " +
            "Make sure Docker is running: docker compose up -d postgres redis");
        throw;
    }
}

// ── Step 2: Create tables inside product_db ───────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProductDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(ProductDbInit.Sql);
        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Product database tables ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to init Product DB tables.");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
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

static class ProductDbInit
{
    public const string Sql = @"
        CREATE TABLE IF NOT EXISTS ""Categories"" (
            ""Id""               UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""Name""             VARCHAR(100) NOT NULL,
            ""Slug""             VARCHAR(120) NOT NULL,
            ""Description""      TEXT,
            ""ParentCategoryId"" UUID,
            ""IsDeleted""        BOOLEAN      NOT NULL DEFAULT FALSE,
            ""CreatedAt""        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            ""UpdatedAt""        TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""          INTEGER      NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Categories_Slug""
            ON ""Categories""(""Slug"");

        CREATE TABLE IF NOT EXISTS ""Products"" (
            ""Id""              UUID             NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""Name""            VARCHAR(200)     NOT NULL,
            ""Description""     TEXT             NOT NULL DEFAULT '',
            ""Sku""             VARCHAR(50)      NOT NULL,
            ""Price""           DECIMAL(18,2)    NOT NULL,
            ""SalePrice""       DECIMAL(18,2),
            ""Currency""        VARCHAR(3)       NOT NULL DEFAULT 'USD',
            ""StockQuantity""   INTEGER          NOT NULL DEFAULT 0,
            ""CategoryId""      UUID             NOT NULL
                                    REFERENCES ""Categories""(""Id""),
            ""Brand""           TEXT,
            ""Status""          VARCHAR(20)      NOT NULL DEFAULT 'Active',
            ""AverageRating""   DOUBLE PRECISION NOT NULL DEFAULT 0,
            ""ReviewCount""     INTEGER          NOT NULL DEFAULT 0,
            ""IsDeleted""       BOOLEAN          NOT NULL DEFAULT FALSE,
            ""CreatedAt""       TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
            ""UpdatedAt""       TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""         INTEGER          NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_Sku""
            ON ""Products""(""Sku"");

        CREATE TABLE IF NOT EXISTS ""ProductImages"" (
            ""Id""          UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""ProductId""   UUID          NOT NULL
                                REFERENCES ""Products""(""Id"") ON DELETE CASCADE,
            ""Url""         VARCHAR(1000) NOT NULL,
            ""IsPrimary""   BOOLEAN       NOT NULL DEFAULT FALSE,
            ""IsDeleted""   BOOLEAN       NOT NULL DEFAULT FALSE,
            ""CreatedAt""   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            ""UpdatedAt""   TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""     INTEGER       NOT NULL DEFAULT 1
        );";
}
