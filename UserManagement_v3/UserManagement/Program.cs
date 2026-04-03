using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Session ───────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opts =>
{
    opts.IdleTimeout         = TimeSpan.FromMinutes(60);
    opts.Cookie.HttpOnly     = true;
    opts.Cookie.IsEssential  = true;
    opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opts.Cookie.Name         = ".UserHub.Session";
});

// ── Services (DI) ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthService,       AuthService>();
builder.Services.AddScoped<IUserService,       UserService>();
builder.Services.AddScoped<IRoleService,       RoleService>();
builder.Services.AddScoped<IModuleService,     ModuleService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Database initialisation ───────────────────────────────────────────────────
//
// Strategy: probe whether tables exist; if not, use EnsureCreated (bypasses
// the migration pipeline entirely so the "already up to date" race cannot occur).
// After schema is ready, DbSeeder.Seed() inserts any missing reference rows and
// guarantees a valid BCrypt password for superadmin.
//
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Step 1 – Probe for the Users table
        bool tablesExist = false;
        try { db.Users.Take(1).ToList(); tablesExist = true; }
        catch { /* table doesn't exist yet */ }

        if (!tablesExist)
        {
            logger.LogInformation("First run – creating database schema...");

            // EnsureCreated creates the full schema (including HasData seed rows)
            // without touching the migrations history table at all.
            db.Database.EnsureCreated();

            logger.LogInformation("Schema created.");
        }
        else
        {
            // Tables already exist – apply any outstanding migrations
            var pending = db.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s)...", pending.Count);
                db.Database.Migrate();
            }
            else
            {
                logger.LogInformation("Database schema is up to date.");
            }
        }

        // Step 2 – Seed reference data and superadmin (idempotent)
        DbSeeder.Seed(db);

        // Step 3 – Verify the superadmin BCrypt hash is valid at runtime
        var admin = db.Users.FirstOrDefault(u => u.Username == "superadmin");
        if (admin != null)
        {
            bool hashOk = false;
            try { hashOk = BCrypt.Net.BCrypt.Verify("Admin@123", admin.PasswordHash); }
            catch { /* bad hash format */ }

            if (!hashOk)
            {
                logger.LogWarning("Superadmin hash invalid – regenerating...");
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                db.SaveChanges();
            }
        }

        logger.LogInformation("Database ready. Login: superadmin / Admin@123");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fatal: database initialisation failed – {Message}", ex.Message);
        throw;
    }
}

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
