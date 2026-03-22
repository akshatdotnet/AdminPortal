using Asp.Versioning;
using Common.Api.Middleware;
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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CouponDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("CouponDb"),
        npg => npg.EnableRetryOnFailure(3)));
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<CreateCouponCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<CreateCouponValidator>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IUnitOfWorkCoupon, UnitOfWorkCoupon>();
var jwt = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer=true, ValidIssuer=jwt["Issuer"], ValidateAudience=true, ValidAudience=jwt["Audience"],
        ValidateIssuerSigningKey=true, IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
        ValidateLifetime=true, ClockSkew=TimeSpan.Zero });
builder.Services.AddAuthorization(opts => opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));
builder.Services.AddApiVersioning(o => { o.DefaultApiVersion=new ApiVersion(1,0); o.AssumeDefaultVersionWhenUnspecified=true; })
    .AddApiExplorer(o => { o.GroupNameFormat="'v'VVV"; o.SubstituteApiVersionInUrl=true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title="Coupon API", Version="v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name="Authorization",Type=SecuritySchemeType.Http,Scheme="Bearer",In=ParameterLocation.Header });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{ new OpenApiSecurityScheme { Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"} }, Array.Empty<string>() }});
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddDbContextCheck<CouponDbContext>("coupon-db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<CouponDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<CouponDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Coupons"" (
                ""Id""                    UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                ""Code""                  VARCHAR(50)   NOT NULL,
                ""Description""           VARCHAR(500)  NOT NULL,
                ""DiscountType""          VARCHAR(20)   NOT NULL DEFAULT 'FixedAmount',
                ""DiscountValue""         DECIMAL(18,2) NOT NULL,
                ""MinimumOrderAmount""    DECIMAL(18,2),
                ""MaximumDiscountAmount"" DECIMAL(18,2),
                ""MaxUsageCount""         INTEGER,
                ""UsageCount""            INTEGER       NOT NULL DEFAULT 0,
                ""ValidFrom""             TIMESTAMPTZ   NOT NULL,
                ""ValidTo""               TIMESTAMPTZ   NOT NULL,
                ""IsActive""              BOOLEAN       NOT NULL DEFAULT TRUE,
                ""IsDeleted""             BOOLEAN       NOT NULL DEFAULT FALSE,
                ""CreatedAt""             TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                ""UpdatedAt""             TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
                ""Version""               INTEGER       NOT NULL DEFAULT 1
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Coupons_Code"" ON ""Coupons""(""Code"");
        ");
        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Coupon database tables ready.");
    }
    catch (Exception ex) { logger.LogError(ex, "Failed to init Coupon DB."); throw; }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coupon API v1"));
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllers(); app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.Run();
