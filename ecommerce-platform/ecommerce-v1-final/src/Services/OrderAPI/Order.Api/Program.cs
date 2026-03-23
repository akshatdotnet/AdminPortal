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
using Order.Application.Commands;
using Order.Application.Interfaces;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("OrderDb"),
        npg => npg.EnableRetryOnFailure(3)));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<PlaceOrderCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<PlaceOrderValidator>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWorkOrder, UnitOfWorkOrder>();
builder.Services.AddScoped<OrderQueryService>();

builder.Services.AddHttpClient<IProductServiceClient, HttpProductServiceClient>(c =>
    c.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:ProductApi"] ?? "http://localhost:5002"));
builder.Services.AddHttpClient<ICouponServiceClient, HttpCouponServiceClient>(c =>
    c.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:CouponApi"] ?? "http://localhost:5005"));

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
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat      = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddDbContextCheck<OrderDbContext>("order-db");

var app = builder.Build();

// -- Step 1: Create order_db if it doesn't exist ---------------
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderDbContext>>();
    var connStr = config.GetConnectionString("OrderDb")!;

    var mainConnStr = new NpgsqlConnectionStringBuilder(connStr)
        { Database = "postgres" }.ConnectionString;

    try
    {
        await using var conn = new NpgsqlConnection(mainConnStr);
        await conn.OpenAsync();
        await using var check = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = 'order_db'", conn);
        var exists = await check.ExecuteScalarAsync();
        if (exists is null)
        {
            await using var create = new NpgsqlCommand("CREATE DATABASE order_db", conn);
            await create.ExecuteNonQueryAsync();
            logger.LogInformation("order_db created.");
        }
        else { logger.LogInformation("order_db already exists."); }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Could not create order_db. Is PostgreSQL running?");
        throw;
    }
}

// -- Step 2: Create tables -------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(OrderDbInit.Sql);
        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Order database tables ready.");
    }
    catch (Exception ex) { logger.LogError(ex, "Failed to init Order DB."); throw; }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1"));
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.Run();

static class OrderDbInit
{
    public const string Sql = @"
        CREATE TABLE IF NOT EXISTS ""Orders"" (
            ""Id""                   UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""OrderNumber""          VARCHAR(50)   NOT NULL,
            ""CustomerId""           UUID          NOT NULL,
            ""Status""               VARCHAR(20)   NOT NULL DEFAULT 'Pending',
            ""PaymentStatus""        VARCHAR(20)   NOT NULL DEFAULT 'Pending',
            ""ShippingAddress_FullName""    VARCHAR(200) NOT NULL DEFAULT '',
            ""ShippingAddress_Street""      VARCHAR(300) NOT NULL DEFAULT '',
            ""ShippingAddress_City""        VARCHAR(100) NOT NULL DEFAULT '',
            ""ShippingAddress_State""       VARCHAR(100) NOT NULL DEFAULT '',
            ""ShippingAddress_PostalCode""  VARCHAR(20)  NOT NULL DEFAULT '',
            ""ShippingAddress_Country""     VARCHAR(100) NOT NULL DEFAULT '',
            ""ShippingAddress_Phone""       VARCHAR(25)  NOT NULL DEFAULT '',
            ""Subtotal""             DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""DiscountAmount""       DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""ShippingCost""         DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""TaxAmount""            DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""Total""                DECIMAL(18,2) NOT NULL DEFAULT 0,
            ""CouponCode""           VARCHAR(50),
            ""Notes""                TEXT,
            ""PaymentIntentId""      TEXT,
            ""PaidAt""               TIMESTAMPTZ,
            ""ShippedAt""            TIMESTAMPTZ,
            ""DeliveredAt""          TIMESTAMPTZ,
            ""CancelledAt""          TIMESTAMPTZ,
            ""TrackingNumber""       VARCHAR(100),
            ""CancellationReason""   TEXT,
            ""IsDeleted""            BOOLEAN       NOT NULL DEFAULT FALSE,
            ""CreatedAt""            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            ""UpdatedAt""            TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""              INTEGER       NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Orders_OrderNumber"" ON ""Orders""(""OrderNumber"");
        CREATE TABLE IF NOT EXISTS ""OrderItems"" (
            ""Id""           UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""OrderId""      UUID          NOT NULL REFERENCES ""Orders""(""Id"") ON DELETE CASCADE,
            ""ProductId""    UUID          NOT NULL,
            ""ProductName""  VARCHAR(200)  NOT NULL,
            ""Sku""          VARCHAR(50)   NOT NULL,
            ""UnitPrice""    DECIMAL(18,2) NOT NULL,
            ""Quantity""     INTEGER       NOT NULL,
            ""IsDeleted""    BOOLEAN       NOT NULL DEFAULT FALSE,
            ""CreatedAt""    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            ""UpdatedAt""    TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""      INTEGER       NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS ""OrderStatusHistories"" (
            ""Id""        UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""OrderId""   UUID         NOT NULL REFERENCES ""Orders""(""Id"") ON DELETE CASCADE,
            ""Status""    VARCHAR(20)  NOT NULL,
            ""Note""      VARCHAR(500) NOT NULL DEFAULT '',
            ""Timestamp"" TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            ""IsDeleted"" BOOLEAN      NOT NULL DEFAULT FALSE,
            ""CreatedAt"" TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            ""UpdatedAt"" TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""   INTEGER      NOT NULL DEFAULT 1
        );";
}
