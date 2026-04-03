using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using STHEnterprise.Blazor;
using STHEnterprise.Blazor.Data;
using STHEnterprise.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

ConfigureRootComponents(builder);
ConfigureServices(builder);

await builder.Build().RunAsync();


// ===============================
// Root Components
// ===============================
static void ConfigureRootComponents(WebAssemblyHostBuilder builder)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}


// ===============================
// Dependency Injection
// ===============================
static void ConfigureServices(WebAssemblyHostBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;

    // HttpClient (API Communication)
    services.AddScoped(sp => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

    // Mock Database (Replace with API later)
    services.AddSingleton<MockDatabase>();

    // Application Services
    services.AddScoped<AuthService>();
    services.AddScoped<ProductService>();
    services.AddScoped<OrderService>();

    // Future Ready Services
    // services.AddScoped<IAuthService, AuthService>();
    // services.AddScoped<IProductService, ProductService>();
    // services.AddScoped<IOrderService, OrderService>();
}







//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using STHEnterprise.Blazor;
//using STHEnterprise.Blazor.Data;
//using STHEnterprise.Blazor.Services;


//var builder = WebAssemblyHostBuilder.CreateDefault(args);
//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");


//builder.Services.AddScoped<AuthService>();
//builder.Services.AddScoped<ProductService>();
//builder.Services.AddScoped<OrderService>();
//builder.Services.AddSingleton<MockDatabase>();

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

//await builder.Build().RunAsync();


/*
 *
 
        MEGA Enterprise Blazor Admin System (like Shopify) with:        
        ✔ JWT Authentication
        ✔ Role Based Access Control
        ✔ Product CRUD
        ✔ Order Workflow
        ✔ Payments
        ✔ Charts (Chart.js)
        ✔ Search + Pagination
        ✔ API Layer (.NET Web API)
        ✔ Microservices Architecture
        ✔ Docker + Kubernetes Ready

 *  Email: admin@sth.com
    Password: admin123
    Role: SuperAdmin
 */



//using STHEnterprise.Blazor.Services;
//using STHEnterprise.Blazor.Data;
//
//var builder = WebApplication.CreateBuilder(args);
//
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();
//
//builder.Services.AddScoped<AuthService>();
//builder.Services.AddScoped<ProductService>();
//builder.Services.AddScoped<OrderService>();
//builder.Services.AddSingleton<MockDatabase>();
//
//var app = builder.Build();
//
//app.UseStaticFiles();
//
//app.MapRazorComponents<App>()
//   .AddInteractiveServerRenderMode();
//
//app.Run();