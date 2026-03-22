using Asp.Versioning;
using Common.Api.Middleware;
using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Product.Application.Commands;
using Product.Application.Interfaces;
using Product.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ProductDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("ProductDb"),
        npg => npg.EnableRetryOnFailure(3)));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateProductCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddScoped<IProductRepository,     ProductRepository>();
builder.Services.AddScoped<ICategoryRepository,    CategoryRepository>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();
builder.Services.AddScoped<IUnitOfWorkProduct,     UnitOfWorkProduct>();
var jwt = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt["Issuer"],
        ValidateAudience = true, ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    });
builder.Services.AddAuthorization(opts => {
    opts.AddPolicy("AdminOnly",     p => p.RequireRole("Admin"));
    opts.AddPolicy("VendorOrAdmin", p => p.RequireRole("Vendor", "Admin"));
});
builder.Services.AddApiVersioning(o => {
    o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true;
}).AddApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Product API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name="Authorization", Type=SecuritySchemeType.Http, Scheme="Bearer", In=ParameterLocation.Header });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{ new OpenApiSecurityScheme { Reference = new OpenApiReference { Type=ReferenceType.SecurityScheme, Id="Bearer" } }, Array.Empty<string>() }});
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddDbContextCheck<ProductDbContext>("product-db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProductDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(DbInit.ProductSql);
        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Product database tables ready.");
    }
    catch (Exception ex) { logger.LogError(ex, "Failed to init Product DB."); throw; }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1"));
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllers(); app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.Run();

static class DbInit
{
    public const string ProductSql = @"
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
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Categories_Slug"" ON ""Categories""(""Slug"");
        CREATE TABLE IF NOT EXISTS ""Products"" (
            ""Id""              UUID            NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""Name""            VARCHAR(200)    NOT NULL,
            ""Description""     TEXT            NOT NULL DEFAULT '',
            ""Sku""             VARCHAR(50)     NOT NULL,
            ""Price""           DECIMAL(18,2)   NOT NULL,
            ""SalePrice""       DECIMAL(18,2),
            ""Currency""        VARCHAR(3)      NOT NULL DEFAULT 'USD',
            ""StockQuantity""   INTEGER         NOT NULL DEFAULT 0,
            ""CategoryId""      UUID            NOT NULL REFERENCES ""Categories""(""Id""),
            ""Brand""           TEXT,
            ""Status""          VARCHAR(20)     NOT NULL DEFAULT 'Active',
            ""AverageRating""   DOUBLE PRECISION NOT NULL DEFAULT 0,
            ""ReviewCount""     INTEGER         NOT NULL DEFAULT 0,
            ""IsDeleted""       BOOLEAN         NOT NULL DEFAULT FALSE,
            ""CreatedAt""       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
            ""UpdatedAt""       TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""         INTEGER         NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_Sku"" ON ""Products""(""Sku"");
        CREATE TABLE IF NOT EXISTS ""ProductImages"" (
            ""Id""          UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            ""ProductId""   UUID          NOT NULL REFERENCES ""Products""(""Id"") ON DELETE CASCADE,
            ""Url""         VARCHAR(1000) NOT NULL,
            ""IsPrimary""   BOOLEAN       NOT NULL DEFAULT FALSE,
            ""IsDeleted""   BOOLEAN       NOT NULL DEFAULT FALSE,
            ""CreatedAt""   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            ""UpdatedAt""   TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
            ""Version""     INTEGER       NOT NULL DEFAULT 1
        );";
}
