using Asp.Versioning;
using Common.Api.Middleware;
using Common.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Payment.Application.Commands;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PaymentDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb"),
        npg => npg.EnableRetryOnFailure(3)));
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<CreatePaymentSessionCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentSessionValidator>();
builder.Services.AddScoped<IPaymentRepository,     PaymentRepository>();
builder.Services.AddScoped<IUnitOfWorkPayment,     UnitOfWorkPayment>();
builder.Services.AddScoped<IPaymentGatewayService, MockPaymentGateway>();
builder.Services.AddHttpClient<IOrderServiceClient, HttpOrderServiceClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["ServiceUrls:OrderApi"] ?? "http://localhost:5003"));
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title="Payment API", Version="v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name="Authorization",Type=SecuritySchemeType.Http,Scheme="Bearer",In=ParameterLocation.Header });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{ new OpenApiSecurityScheme { Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"} }, Array.Empty<string>() }});
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentDbContext>("payment-db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentDbContext>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Payments"" (
                ""Id""               UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                ""OrderId""          UUID          NOT NULL,
                ""CustomerId""       UUID          NOT NULL,
                ""Amount""           DECIMAL(18,2) NOT NULL,
                ""RefundAmount""     DECIMAL(18,2),
                ""Currency""         VARCHAR(3)    NOT NULL DEFAULT 'USD',
                ""Status""           VARCHAR(20)   NOT NULL DEFAULT 'Pending',
                ""Gateway""          VARCHAR(20)   NOT NULL DEFAULT 'Stripe',
                ""GatewayPaymentId"" VARCHAR(200),
                ""GatewaySessionId"" VARCHAR(200),
                ""CheckoutUrl""      VARCHAR(1000),
                ""FailureReason""    TEXT,
                ""ProcessedAt""      TIMESTAMPTZ,
                ""IsDeleted""        BOOLEAN       NOT NULL DEFAULT FALSE,
                ""CreatedAt""        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                ""UpdatedAt""        TIMESTAMPTZ, ""CreatedBy"" TEXT, ""UpdatedBy"" TEXT,
                ""Version""          INTEGER       NOT NULL DEFAULT 1
            );
        ");
        await db.Database.CloseConnectionAsync();
        logger.LogInformation("Payment database tables ready.");
    }
    catch (Exception ex) { logger.LogError(ex, "Failed to init Payment DB."); throw; }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1"));
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllers(); app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.Run();
