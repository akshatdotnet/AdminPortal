using Zovo.Application;
using Zovo.Infrastructure;
using Zovo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────
builder.Services.AddControllersWithViews().AddNewtonsoftJson();

// useInMemory: true  → runs instantly, no SQL Server needed
// useInMemory: false → uses appsettings.json ConnectionStrings:DefaultConnection
builder.Services.AddInfrastructure(builder.Configuration, useInMemory: true);
builder.Services.AddApplication();

var app = builder.Build();

// ── Seed the in-memory database ───────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ZovoDbContext>();
    db.Database.EnsureCreated();
}

// ── Pipeline ──────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
