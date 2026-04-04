using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RoleModulePermission> RoleModulePermissions => Set<RoleModulePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<RoleModulePermission>()
                .HasOne(rmp => rmp.Role)
                .WithMany(r => r.RoleModulePermissions)
                .HasForeignKey(rmp => rmp.RoleId);

            modelBuilder.Entity<RoleModulePermission>()
                .HasOne(rmp => rmp.Module)
                .WithMany(m => m.RoleModulePermissions)
                .HasForeignKey(rmp => rmp.ModuleId);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();

            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Module>().HasData(
                new Module { Id = 1, Name = "Dashboard",         ControllerName = "Home",    Icon = "bi-speedometer2",    SortOrder = 1, Description = "Main dashboard",         IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Module { Id = 2, Name = "User Management",   ControllerName = "Users",   Icon = "bi-people-fill",     SortOrder = 2, Description = "Manage system users",   IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Module { Id = 3, Name = "Role Management",   ControllerName = "Roles",   Icon = "bi-shield-check",    SortOrder = 3, Description = "Manage roles",           IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Module { Id = 4, Name = "Module Management", ControllerName = "Modules", Icon = "bi-grid-3x3-gap",    SortOrder = 4, Description = "Manage modules",         IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "SuperAdmin", Description = "Full system access",      IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = 2, Name = "Admin",      Description = "Administrative access",   IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = 3, Name = "Viewer",     Description = "Read-only access",        IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<RoleModulePermission>().HasData(
                // SuperAdmin - full access
                new RoleModulePermission { Id = 1,  RoleId = 1, ModuleId = 1, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { Id = 2,  RoleId = 1, ModuleId = 2, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { Id = 3,  RoleId = 1, ModuleId = 3, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                new RoleModulePermission { Id = 4,  RoleId = 1, ModuleId = 4, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                // Admin
                new RoleModulePermission { Id = 5,  RoleId = 2, ModuleId = 1, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false },
                new RoleModulePermission { Id = 6,  RoleId = 2, ModuleId = 2, CanView = true,  CanCreate = true,  CanEdit = true,  CanDelete = false },
                new RoleModulePermission { Id = 7,  RoleId = 2, ModuleId = 3, CanView = true,  CanCreate = true,  CanEdit = true,  CanDelete = false },
                // Viewer
                new RoleModulePermission { Id = 8,  RoleId = 3, ModuleId = 1, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false },
                new RoleModulePermission { Id = 9,  RoleId = 3, ModuleId = 2, CanView = true,  CanCreate = false, CanEdit = false, CanDelete = false }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "Super Administrator",
                    Email = "superadmin@system.com",
                    Username = "superadmin",
                    PasswordHash = "$2a$11$5hMjFHasMHrFDPO7M9yp8.RPwL6ot5hLgLsVZ6rXJlJljcn1O5OEO",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1, AssignedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
