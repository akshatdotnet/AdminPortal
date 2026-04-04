using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Data
{
    /// <summary>
    /// Called after EnsureCreated() to verify / insert seed rows at runtime.
    /// This guarantees the superadmin user always has a valid BCrypt password hash
    /// and all reference data is present regardless of how the DB was created.
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            SeedModules(db);
            SeedRoles(db);
            SeedPermissions(db);
            SeedSuperAdmin(db);
            db.SaveChanges();
        }

        private static void SeedModules(AppDbContext db)
        {
            if (db.Modules.Any()) return;

            db.Modules.AddRange(
                new Module { Id = 1, Name = "Dashboard",         ControllerName = "Home",    Icon = "bi-speedometer2", SortOrder = 1, Description = "Main dashboard",       IsActive = true, CreatedAt = DateTime.UtcNow },
                new Module { Id = 2, Name = "User Management",   ControllerName = "Users",   Icon = "bi-people-fill",  SortOrder = 2, Description = "Manage system users", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Module { Id = 3, Name = "Role Management",   ControllerName = "Roles",   Icon = "bi-shield-check", SortOrder = 3, Description = "Manage roles",         IsActive = true, CreatedAt = DateTime.UtcNow },
                new Module { Id = 4, Name = "Module Management", ControllerName = "Modules", Icon = "bi-grid-3x3-gap", SortOrder = 4, Description = "Manage modules",       IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            db.SaveChanges();
        }

        private static void SeedRoles(AppDbContext db)
        {
            if (db.Roles.Any()) return;

            db.Roles.AddRange(
                new Role { Id = 1, Name = "SuperAdmin", Description = "Full system access",    IsActive = true, CreatedAt = DateTime.UtcNow },
                new Role { Id = 2, Name = "Admin",      Description = "Administrative access", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Role { Id = 3, Name = "Viewer",     Description = "Read-only access",      IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            db.SaveChanges();
        }

        private static void SeedPermissions(AppDbContext db)
        {
            if (db.RoleModulePermissions.Any()) return;

            db.RoleModulePermissions.AddRange(
                // SuperAdmin — full access to all modules
                new RoleModulePermission { RoleId = 1, ModuleId = 1, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { RoleId = 1, ModuleId = 2, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { RoleId = 1, ModuleId = 3, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { RoleId = 1, ModuleId = 4, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                // Admin — can manage users & roles but not delete
                new RoleModulePermission { RoleId = 2, ModuleId = 1, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false },
                new RoleModulePermission { RoleId = 2, ModuleId = 2, CanView = true,  CanCreate = true,  CanEdit = true,  CanDelete = false },
                new RoleModulePermission { RoleId = 2, ModuleId = 3, CanView = true,  CanCreate = true,  CanEdit = true,  CanDelete = false },
                // Viewer — read-only
                new RoleModulePermission { RoleId = 3, ModuleId = 1, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false },
                new RoleModulePermission { RoleId = 3, ModuleId = 2, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false }
            );
            db.SaveChanges();
        }

        private static void SeedSuperAdmin(AppDbContext db)
        {
            if (db.Users.Any(u => u.Username == "superadmin")) return;

            var user = new User
            {
                FullName     = "Super Administrator",
                Email        = "superadmin@system.com",
                Username     = "superadmin",
                // Hash generated at runtime — always valid
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();

            // Assign SuperAdmin role
            if (!db.UserRoles.Any(ur => ur.UserId == user.Id))
            {
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = 1, AssignedAt = DateTime.UtcNow });
                db.SaveChanges();
            }
        }
    }
}
