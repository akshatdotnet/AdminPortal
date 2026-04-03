using System.ComponentModel.DataAnnotations;

namespace UserManagement.ViewModels
{
    // ── Auth ─────────────────────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    // ── Session ───────────────────────────────────────────────────────────────

    public class SessionUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public List<ModulePermissionRow> Permissions { get; set; } = new List<ModulePermissionRow>();
    }

    // ── User ──────────────────────────────────────────────────────────────────

    public class UserListViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Assigned Roles")]
        public List<int> SelectedRoleIds { get; set; } = new List<int>();

        public List<RoleCheckboxItem> AvailableRoles { get; set; } = new List<RoleCheckboxItem>();
    }

    public class UserDetailViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<RolePermissionSummary> Permissions { get; set; } = new List<RolePermissionSummary>();
    }

    public class RoleCheckboxItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class RolePermissionSummary
    {
        public string RoleName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    // ── Role ──────────────────────────────────────────────────────────────────

    public class RoleListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RoleCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<ModulePermissionRow> ModulePermissions { get; set; } = new List<ModulePermissionRow>();
    }

    // ── Module ────────────────────────────────────────────────────────────────

    public class ModuleListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ControllerName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ModuleCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Module name is required")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Controller name is required")]
        [MaxLength(50)]
        [Display(Name = "Controller Name")]
        public string ControllerName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Icon Class")]
        public string Icon { get; set; } = "bi-grid";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    public class ModulePermissionRow
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ModuleIcon { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class SearchFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; } = "asc";
    }
}
