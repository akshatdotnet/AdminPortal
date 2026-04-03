using System.Collections.Concurrent;

public static class InMemoryUserStore
{
    public static ConcurrentDictionary<string, AppUser> Users { get; }
        = new ConcurrentDictionary<string, AppUser>();

    // Static constructor → runs once on app start
    static InMemoryUserStore()
    {
        SeedUsers();
    }

    private static void SeedUsers()
    {
        AddUser(
            fullName: "System Admin",
            mobile:"8097944981",
            email: "admin@sth.com",
            password: "Admin@123",
            role: "Admin"
        );

        AddUser(
            fullName: "Operations Manager",
            mobile: "8097944981",
            email: "manager@sth.com",
            password: "Manager@123",
            role: "Manager"
        );

        AddUser(
            fullName: "Normal User",
            mobile: "8097944981",
            email: "user@sth.com",
            password: "User@123",
            role: "User"
        );
    }

    private static void AddUser(
        string fullName,
        string mobile,
        string email,
        string password,
        string role)
    {
        if (Users.ContainsKey(email))
            return;

        var user = new AppUser
        {
            Name = fullName,
            PhoneNumber = mobile,
            Email = email,
            Role = role,
            PasswordHash = PasswordHasher.Hash(password)
        };

        Users[email] = user;
    }
}
