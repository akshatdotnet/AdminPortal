namespace UserManagement.Models
{
    public class RoleModulePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int ModuleId { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public Role Role { get; set; } = null!;
        public Module Module { get; set; } = null!;
    }
}
