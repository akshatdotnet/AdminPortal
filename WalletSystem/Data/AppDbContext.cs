using Microsoft.EntityFrameworkCore;
using WalletSystem.Models;

namespace WalletSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    //protected override void OnModelCreating(ModelBuilder mb)
    //{
    //    // User
    //    mb.Entity<User>(e =>
    //    {
    //        e.HasIndex(u => u.Email).IsUnique();
    //        e.HasIndex(u => u.Username).IsUnique();
    //        e.Property(u => u.Status).HasConversion<string>();
    //    });

    //    // Wallet
    //    mb.Entity<Wallet>(e =>
    //    {
    //        e.HasIndex(w => w.UserId).IsUnique();
    //        e.Property(w => w.Balance).HasPrecision(18, 2);
    //        e.Property(w => w.Status).HasConversion<string>();
    //        e.HasOne(w => w.User).WithOne(u => u.Wallet)
    //         .HasForeignKey<Wallet>(w => w.UserId);
    //    });

    //    // Transaction
    //    mb.Entity<Transaction>(e =>
    //    {
    //        e.HasIndex(t => t.ReferenceNumber).IsUnique();
    //        e.Property(t => t.Amount).HasPrecision(18, 2);
    //        e.Property(t => t.BalanceBefore).HasPrecision(18, 2);
    //        e.Property(t => t.BalanceAfter).HasPrecision(18, 2);
    //        e.Property(t => t.Type).HasConversion<string>();
    //        e.Property(t => t.Status).HasConversion<string>();
    //        e.HasOne(t => t.Wallet).WithMany(w => w.Transactions)
    //         .HasForeignKey(t => t.WalletId).OnDelete(DeleteBehavior.Restrict);
    //        e.HasOne(t => t.RelatedWallet).WithMany()
    //         .HasForeignKey(t => t.RelatedWalletId).OnDelete(DeleteBehavior.Restrict);
    //    });

    //    // Seed data
    //    SeedData(mb);
    //}

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Status).HasConversion<string>();
        });

        // Wallet
        mb.Entity<Wallet>(e =>
        {
            e.HasIndex(w => w.UserId).IsUnique();
            e.Property(w => w.Balance).HasPrecision(18, 2);
            //e.Property(w => w.Balance).HasColumnType("REAL").HasPrecision(18, 2);
            e.Property(w => w.Status).HasConversion<string>();
            e.HasOne(w => w.User).WithOne(u => u.Wallet)
             .HasForeignKey<Wallet>(w => w.UserId);
        });

        // Transaction
        mb.Entity<Transaction>(e =>
        {
            e.HasIndex(t => t.ReferenceNumber).IsUnique();
            //e.Property(t => t.Amount).HasColumnType("REAL").HasPrecision(18, 2);
            //e.Property(t => t.BalanceBefore).HasColumnType("REAL").HasPrecision(18, 2);
            //e.Property(t => t.BalanceAfter).HasColumnType("REAL").HasPrecision(18, 2);
            e.Property(t => t.Amount).HasColumnType("REAL").HasPrecision(18, 2);
            e.Property(t => t.BalanceBefore).HasPrecision(18, 2);
            e.Property(t => t.BalanceAfter).HasPrecision(18, 2);
            e.Property(t => t.Type).HasConversion<string>();
            e.Property(t => t.Status).HasConversion<string>();
            e.HasOne(t => t.Wallet).WithMany(w => w.Transactions)
             .HasForeignKey(t => t.WalletId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.RelatedWallet).WithMany()
             .HasForeignKey(t => t.RelatedWalletId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(mb);
    }

    private static void SeedData(ModelBuilder mb)
    {
        var users = new List<User>
        {
            new() { Id=1, FullName="Alice Johnson",   Email="alice@example.com",   PhoneNumber="555-0101", Username="alice",   Status=UserStatus.Active,   CreatedAt=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new() { Id=2, FullName="Bob Martinez",    Email="bob@example.com",     PhoneNumber="555-0102", Username="bob",     Status=UserStatus.Active,   CreatedAt=new DateTime(2024,1,2,0,0,0,DateTimeKind.Utc) },
            new() { Id=3, FullName="Carol White",     Email="carol@example.com",   PhoneNumber="555-0103", Username="carol",   Status=UserStatus.Inactive, CreatedAt=new DateTime(2024,1,3,0,0,0,DateTimeKind.Utc) },
            new() { Id=4, FullName="David Lee",       Email="david@example.com",   PhoneNumber="555-0104", Username="david",   Status=UserStatus.Active,   CreatedAt=new DateTime(2024,1,4,0,0,0,DateTimeKind.Utc) },
            new() { Id=5, FullName="Emma Davis",      Email="emma@example.com",    PhoneNumber="555-0105", Username="emma",    Status=UserStatus.Suspended, CreatedAt=new DateTime(2024,1,5,0,0,0,DateTimeKind.Utc) },
        };
        mb.Entity<User>().HasData(users);   

        var wallets = new List<Wallet>
        {
            new() { Id=1, UserId=1, Balance=1500.00m, Currency="USD", Status=WalletStatus.Active,  CreatedAt=new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc) },
            new() { Id=2, UserId=2, Balance=320.50m,  Currency="USD", Status=WalletStatus.Active,  CreatedAt=new DateTime(2024,1,2,0,0,0,DateTimeKind.Utc) },
            new() { Id=3, UserId=3, Balance=0.00m,    Currency="USD", Status=WalletStatus.Frozen,  CreatedAt=new DateTime(2024,1,3,0,0,0,DateTimeKind.Utc) },
            new() { Id=4, UserId=4, Balance=9800.75m, Currency="USD", Status=WalletStatus.Active,  CreatedAt=new DateTime(2024,1,4,0,0,0,DateTimeKind.Utc) },
            new() { Id=5, UserId=5, Balance=50.00m,   Currency="USD", Status=WalletStatus.Frozen,  CreatedAt=new DateTime(2024,1,5,0,0,0,DateTimeKind.Utc) },
        };
        mb.Entity<Wallet>().HasData(wallets);

        var txns = new List<Transaction>
        {
            new() { Id=1,  WalletId=1, Type=TransactionType.Deposit,    Amount=2000m,   BalanceBefore=0,       BalanceAfter=2000m,   Description="Initial deposit",     ReferenceNumber="REF-001", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,1,1,9,0,0,DateTimeKind.Utc) },
            new() { Id=2,  WalletId=1, Type=TransactionType.Withdrawal,  Amount=500m,    BalanceBefore=2000m,   BalanceAfter=1500m,   Description="ATM withdrawal",       ReferenceNumber="REF-002", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,1,10,0,0,0,DateTimeKind.Utc) },
            new() { Id=3,  WalletId=2, Type=TransactionType.Deposit,     Amount=500m,    BalanceBefore=0,       BalanceAfter=500m,    Description="Bank transfer in",     ReferenceNumber="REF-003", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,1,2,9,0,0,DateTimeKind.Utc) },
            new() { Id=4,  WalletId=2, Type=TransactionType.Withdrawal,  Amount=179.50m, BalanceBefore=500m,    BalanceAfter=320.50m, Description="Online purchase",      ReferenceNumber="REF-004", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,1,15,0,0,0,DateTimeKind.Utc) },
            new() { Id=5,  WalletId=4, Type=TransactionType.Deposit,     Amount=10000m,  BalanceBefore=0,       BalanceAfter=10000m,  Description="Wire transfer",        ReferenceNumber="REF-005", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,1,4,9,0,0,DateTimeKind.Utc) },
            new() { Id=6,  WalletId=4, Type=TransactionType.Fee,         Amount=199.25m, BalanceBefore=10000m,  BalanceAfter=9800.75m,Description="Monthly fee",          ReferenceNumber="REF-006", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,2,1,0,0,0,DateTimeKind.Utc) },
            new() { Id=7,  WalletId=1, Type=TransactionType.Transfer,    Amount=100m,    BalanceBefore=1500m,   BalanceAfter=1400m,   Description="Transfer to Bob",      ReferenceNumber="REF-007", Status=TransactionStatus.Completed, RelatedWalletId=2, CreatedAt=new DateTime(2024,2,5,0,0,0,DateTimeKind.Utc) },
            new() { Id=8,  WalletId=2, Type=TransactionType.Transfer,    Amount=100m,    BalanceBefore=320.50m, BalanceAfter=420.50m, Description="Transfer from Alice",  ReferenceNumber="REF-008", Status=TransactionStatus.Completed, RelatedWalletId=1, CreatedAt=new DateTime(2024,2,5,0,0,0,DateTimeKind.Utc) },
            new() { Id=9,  WalletId=1, Type=TransactionType.Bonus,       Amount=50m,     BalanceBefore=1400m,   BalanceAfter=1450m,   Description="Referral bonus",       ReferenceNumber="REF-009", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,2,10,0,0,0,DateTimeKind.Utc) },
            new() { Id=10, WalletId=1, Type=TransactionType.Refund,      Amount=50m,     BalanceBefore=1450m,   BalanceAfter=1500m,   Description="Refund from merchant", ReferenceNumber="REF-010", Status=TransactionStatus.Completed, CreatedAt=new DateTime(2024,2,15,0,0,0,DateTimeKind.Utc) },
        };
        mb.Entity<Transaction>().HasData(txns);
    }
}
