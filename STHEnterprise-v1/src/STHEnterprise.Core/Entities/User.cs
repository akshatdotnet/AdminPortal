using System.Data;

namespace STHEnterprise.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = "";
    public string Name { get; set; } = "Guest User";

}

//======================

public class AppUser : BaseEntity
{
    public string FirstName { get; set; } = default!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = default!;

    public string DisplayName { get; set; } = "Guest User";

    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;    
    public string NormalizedPhoneNumber { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;
    public string? MpinHash { get; set; }

    public DateTime DOB { get; set; }

    public bool EmailVerified { get; set; }
    public bool MobileVerified { get; set; }

    public string? ProfilePhotoUrl { get; set; }
    public DateTime? LastLoginDate { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public string FullName => $"{FirstName} {LastName}";
}

public class Role : BaseEntity
{
    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!; // Example: USER_ADD

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";

    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public byte[]? RowVersion { get; set; } // Concurrency control
}


public static class Permissions
{
    public const string UserAdd = "USER_ADD";
    public const string UserEdit = "USER_EDIT";
    public const string UserView = "USER_VIEW";
    public const string UserDelete = "USER_DELETE";
}

/*
 * 
1️⃣ Full EF Migration with seed data
2️⃣ JWT with Role + Permission claims
3️⃣ Policy-based authorization
4️⃣ Dynamic permission middleware
5️⃣ Complete Clean Architecture folder structure

1️⃣ JWT with Role + Permission claims implementation
2️⃣ Policy-based authorization example
3️⃣ Dynamic permission middleware
4️⃣ Full Auth microservice architecture
5️⃣ Database migration + seed strategy

 */

//public enum UserRole
//{
//    Admin = 1,
//    Manager = 2,
//    ChannelPartner = 3,
//    Client = 4
//}
