# UserHub — ASP.NET Core 8 MVC User Management System

A production-ready, session-based user management system built with ASP.NET Core 8 MVC,
Entity Framework Core 8, BCrypt password hashing, and role-based module permissions.

---

## ⚡ Quick Start

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server Express (LocalDB works out of the box)
- Visual Studio 2022 / VS Code / Rider

### 2. Configure the connection string
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=UserManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```
For SQL Server Express use: `Server=.\\SQLEXPRESS;Database=UserManagementDb;Trusted_Connection=True`

### 3. Run migrations
```bash
cd UserManagement
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **Note:** The app also calls `db.Database.Migrate()` automatically on startup,
> so the database will be created/updated every time you run.

### 4. Run
```bash
dotnet run
```
Navigate to `https://localhost:5001` (or `http://localhost:5000`)

### 5. Default login
| Field    | Value                  |
|----------|------------------------|
| Username | `superadmin`           |
| Password | `Admin@123`            |
| Email    | `superadmin@system.com`|

---

## 📁 Project Structure

```
UserManagement/
│
├── Controllers/
│   ├── AccountController.cs      # Login / Logout
│   ├── HomeController.cs         # Dashboard (stats)
│   ├── UsersController.cs        # CRUD + Detail + Paging
│   ├── RolesController.cs        # CRUD + Permission matrix
│   └── ModulesController.cs      # CRUD + Sort order
│
├── Data/
│   └── AppDbContext.cs           # EF Core DbContext + HasData seed
│
├── Filters/
│   ├── RequireLoginAttribute.cs  # Redirects to login if no session
│   └── RequirePermissionAttribute.cs  # Checks role-module permission
│
├── Helpers/
│   └── SessionExtensions.cs     # GetObjectFromJson / SetObjectAsJson
│
├── Models/
│   ├── User.cs
│   ├── Role.cs
│   ├── Module.cs
│   ├── UserRole.cs               # Many-to-many join
│   └── RoleModulePermission.cs   # Role → Module → CRUD flags
│
├── Services/
│   ├── AuthService.cs            # Login validation, last-login update
│   ├── UserService.cs            # User CRUD + paged list
│   ├── RoleService.cs            # Role CRUD + permission sync
│   ├── ModuleService.cs          # Module CRUD
│   └── PermissionService.cs      # Union of permissions across roles
│
├── ViewModels/
│   └── ViewModels.cs             # All VMs: Login, User, Role, Module,
│                                 #          Session, Search/Filter, Perm
│
├── Views/
│   ├── Account/Login.cshtml      # Standalone (no layout)
│   ├── Home/Index.cshtml         # Dashboard cards + quick actions
│   ├── Users/
│   │   ├── Index.cshtml          # Paginated table, search, sort
│   │   ├── CreateEdit.cshtml     # Shared form (create + edit)
│   │   └── Detail.cshtml         # Profile + permissions table
│   ├── Roles/
│   │   ├── Index.cshtml          # Paginated table + user count
│   │   └── CreateEdit.cshtml     # Form + interactive permission matrix
│   ├── Modules/
│   │   ├── Index.cshtml          # Paginated table
│   │   └── CreateEdit.cshtml     # Form + live icon preview
│   └── Shared/
│       ├── _Layout.cshtml        # Sidebar, topbar, alerts, session nav
│       ├── AccessDenied.cshtml
│       └── _ValidationScriptsPartial.cshtml
│
├── wwwroot/
│   ├── css/site.css              # Full dark theme (CSS variables)
│   └── js/site.js                # Sidebar toggle, alerts, delete confirm
│
├── Program.cs                    # DI, middleware, auto-migrate, seed fix
├── appsettings.json
└── UserManagement.csproj
```

---

## ✨ Feature List

### 🔐 Authentication
- Session-based login/logout (no Identity framework)
- BCrypt password hashing (cost factor 11)
- Return URL redirect after login
- HttpOnly + SameSite session cookies
- Last login timestamp tracking
- Auto-dismiss alerts after 4 seconds

### 👤 User Management
- **List** — server-side paging (10/25/50 per page), search by name/email/username,
  filter by active/inactive, sort by any column (asc/desc)
- **Create** — full validation, role assignment checkboxes, password + confirm
- **Edit** — same form, password field optional (blank = keep current)
- **Delete** — POST with CSRF token, confirmation dialog, protects ID=1 (superadmin)
- **Detail** — profile card + full permission matrix per role/module

### 🛡️ Role Management
- **List** — paged, searchable, shows user count per role
- **Create/Edit** — permission matrix (View/Create/Edit/Delete per module),
  Select All / Clear All shortcuts
- **Delete** — blocked if any users are assigned the role

### 🧩 Module Management
- Register any MVC controller as a module for permission control
- Live Bootstrap icon preview while typing icon class
- Sort order for sidebar display
- Deleting a module cascades to remove its RoleModulePermissions

### 🔑 Permission System
- **Model**: Role → Module → {CanView, CanCreate, CanEdit, CanDelete}
- **Multi-role merge**: user with multiple roles gets the **union** of all permissions
- **SuperAdmin bypass**: always has full access regardless of permissions
- **`[RequireLogin]`** attribute on controller class — redirects to login
- **`[RequirePermission("Edit", "Users")]`** attribute on action — shows Access Denied
- Sidebar navigation auto-hides inaccessible modules per session

### 🎨 UI
- Dark theme with CSS custom properties throughout
- Responsive sidebar (collapses to hamburger on mobile)
- Sortable column headers with direction indicators
- Color-coded status badges (Active/Inactive)
- Role tag badges, avatar initials
- Toast alerts (auto-dismiss after 4s)
- Delete confirmation dialog

---

## 🔧 Best Practices Implemented

| Practice | Implementation |
|----------|----------------|
| CSRF protection | `[ValidateAntiForgeryToken]` on every POST |
| Password security | BCrypt with cost 11, never stored plain |
| No GET deletes | All deletes are POST forms |
| Unique DB constraints | Email + Username indexed as unique |
| Guard clauses | Service layer validates before DB ops |
| Interface-driven DI | All services registered via interfaces |
| Async throughout | All DB calls use `async/await` |
| Eager loading | `.Include().ThenInclude()` to avoid N+1 |
| Soft delete | `IsActive` flag — no accidental hard delete |
| Session security | HttpOnly, SameSite, 60-min idle timeout |
| Input validation | DataAnnotations + jQuery Unobtrusive on client |
| Seed data | `HasData()` migration-safe seeding |
| Startup safety | Auto-rehash seed password if invalid |

---

## 📦 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.11 | ORM + SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.11 | `dotnet ef` CLI commands |
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `X.PagedList` | 10.0.1 | Pagination model |
| `X.PagedList.Mvc.Core` | 10.0.1 | Razor pager helper |
| `X.PagedList.EF` | 10.0.1 | `ToPagedListAsync()` extension |
| `System.Text.Json` | 8.0.5 | Session serialization (patched) |

---

## 🗄️ Database Schema

```
Users
  Id, FullName, Email (unique), Username (unique),
  PasswordHash, IsActive, CreatedAt, UpdatedAt, LastLoginAt

Roles
  Id, Name (unique), Description, IsActive, CreatedAt

Modules
  Id, Name, Description, ControllerName, Icon, SortOrder, IsActive, CreatedAt

UserRole  (join table)
  UserId FK → Users.Id
  RoleId FK → Roles.Id
  AssignedAt

RoleModulePermission
  Id, RoleId FK → Roles.Id, ModuleId FK → Modules.Id
  CanView, CanCreate, CanEdit, CanDelete
```

---

## 🌱 Seeded Data

| Entity | Seeded Records |
|--------|---------------|
| Modules | Dashboard, User Management, Role Management, Module Management |
| Roles | SuperAdmin (full), Admin (no delete), Viewer (view only) |
| Users | superadmin / Admin@123 (SuperAdmin role) |

---

## 🚀 Extending the System

**Add a new module** (e.g. Products):
1. Create `ProductsController.cs` with `[RequireLogin]` on the class
2. Add `[RequirePermission("Create")]` / `[RequirePermission("Delete")]` on actions
3. In the app, go to **Modules → Add Module**, set ControllerName = `Products`
4. Go to **Roles → Edit** each role, tick the permissions for Products
5. The sidebar link will appear automatically for users who have View access

**Add a new role**:
1. Go to **Roles → Add Role**
2. Fill in name, description, tick module permissions
3. Assign to users via **Users → Edit**

**Change session timeout**:
Edit `Program.cs` → `opts.IdleTimeout = TimeSpan.FromMinutes(60);`
