using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using STHEnterprise.Api.Helpers;
using STHEnterprise.Application.Interfaces;
using STHEnterprise.Infrastructure;
using STHEnterprise.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region ===================== Services =====================

// --------------------
// Controllers (API only)
// --------------------
builder.Services.AddControllers();

// --------------------
// Application Services
// --------------------
//builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddInfrastructure();


// --------------------
// HTTP Context Accessor
// --------------------
builder.Services.AddHttpContextAccessor();

// --------------------
// Distributed Cache (REQUIRED for Session)
// --------------------
builder.Services.AddDistributedMemoryCache();

// --------------------
// Session Configuration
// --------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --------------------
// API Versioning
// --------------------
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

//.AddApiExplorer(options =>
//{
//    options.GroupNameFormat = "'v'VVV";
//    options.SubstituteApiVersionInUrl = true;
//});


// --------------------
// JWT Token Generator
// --------------------
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// --------------------
// Authentication (JWT Bearer)
// --------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

            ClockSkew = TimeSpan.Zero
        };
    });

// --------------------
// Authorization
// --------------------
builder.Services.AddAuthorization();



// --------------------
// Swagger / OpenAPI
// --------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "STHEnterprise API",
        Version = "v1",
        Description = "Enterprise-grade API with JWT Authentication"
    });

    // JWT Support in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

#endregion

var app = builder.Build();

#region ===================== Middleware Pipeline =====================

// --------------------
// Swagger
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "STHEnterprise API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// --------------------
// Static Files (if needed later)
// --------------------
app.UseStaticFiles();

app.UseRouting();

// --------------------
// Session (BEFORE Auth)
// --------------------
app.UseSession();

// --------------------
// Authentication & Authorization
// --------------------
app.UseAuthentication();
app.UseAuthorization();

// --------------------
// Map Controllers
// --------------------
app.MapControllers();

#endregion

app.Run();


//using Asp.Versioning;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using STHEnterprise.Api.Helpers;
//using System.Text;
//
//var builder = WebApplication.CreateBuilder(args);
//
//#region Services
//
//// --------------------
//// Controllers
//// --------------------
//builder.Services.AddControllers();
//
//// --------------------
//// API Versioning
//// --------------------
//builder.Services.AddApiVersioning(options =>
//{
//    options.DefaultApiVersion = new ApiVersion(1, 0);
//    options.AssumeDefaultVersionWhenUnspecified = true;
//    options.ReportApiVersions = true;
//});
//
//builder.Services.AddHttpContextAccessor();
//builder.Services.AddSession();
//.AddApiExplorer(options =>
//{
//    options.GroupNameFormat = "'v'VVV";
//    options.SubstituteApiVersionInUrl = true;
//});

// --------------------
// JWT Authentication
// --------------------
//builder.Services.AddScoped<JwtTokenGenerator>();
//builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
//
//
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
//
//            ClockSkew = TimeSpan.Zero // IMPORTANT
//        };
//    });

// --------------------
// Authorization (Roles + Permissions)
// --------------------
builder.Services.AddAuthorization();

// Permission-based auth
//builder.Services.AddSingleton<IAuthorizationHandler,
//    PermissionAuthorizationHandler>();

//builder.Services.AddAuthorization(options =>
//{
//    foreach (var permission in RolePermissions.Map
//                 .SelectMany(x => x.Value)
//                 .Distinct())
//    {
//        options.AddPolicy(permission, policy =>
//            policy.Requirements.Add(
//                new PermissionRequirement(permission)));
//    }
//});

// --------------------
// Swagger / OpenAPI
// --------------------
//builder.Services.AddEndpointsApiExplorer();
//
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "STHEnterprise API",
//        Version = "v1",
//        Description = "Enterprise-grade API with JWT, Roles & Permissions"
//    });
//
//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Enter: Bearer {your JWT token}"
//    });
//
//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});
//
//#endregion
//
//var app = builder.Build();
//
//#region Middleware pipeline
//
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(options =>
//    {
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "STHEnterprise API v1");
//        options.RoutePrefix = "swagger";
//    });
//}
//else
//{
//    app.UseHsts();
//}
//
//app.UseHttpsRedirection();
//
//app.UseRouting();
//app.UseSession();
//app.UseAuthorization();
//app.MapControllers();
//
//#endregion
//
//app.Run();









//using Asp.Versioning;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;
////using STHEnterprise.Infrastructure.Data;

//var builder = WebApplication.CreateBuilder(args);

//#region Services

//// Controllers
//builder.Services.AddControllers();

//// API Versioning
//builder.Services.AddApiVersioning(options =>
//{
//    options.DefaultApiVersion = new ApiVersion(1, 0);
//    options.AssumeDefaultVersionWhenUnspecified = true;
//    options.ReportApiVersions = true;
//});

////=============
//builder.Services.AddScoped<JwtTokenGenerator>();

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
//        };
//    });

//builder.Services.AddAuthorization();
////========


////.AddApiExplorer(options =>
////{
////    options.GroupNameFormat = "'v'VVV";
////    options.SubstituteApiVersionInUrl = true;
////});

//// Swagger / OpenAPI
//builder.Services.AddEndpointsApiExplorer();
////builder.Services.AddSwaggerGen(options =>
////{
////    options.SwaggerDoc("v1", new OpenApiInfo
////    {
////        Title = "STHEnterprise API",
////        Version = "v1",
////        Description = "Enterprise-grade API for STHEnterprise"
////    });
////});

//builder.Services.AddSwaggerGen(options =>
//{
//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Enter: Bearer {your JWT token}"
//    });

//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});


//// Infrastructure services (example)
//// builder.Services.AddDbContext<AppDbContext>(...);
//// builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

//#endregion

//var app = builder.Build();

//#region Middleware pipeline

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(options =>
//    {
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "STHEnterprise API v1");
//        options.RoutePrefix = "swagger";
//    });
//}

//app.UseHttpsRedirection();

//app.UseAuthentication(); // enable when JWT added
//app.UseAuthorization();

//app.MapControllers();

//#endregion

//app.Run();
