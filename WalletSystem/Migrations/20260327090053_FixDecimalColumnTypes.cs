using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WalletSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WalletId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedWalletId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "REAL", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Wallets_RelatedWalletId",
                        column: x => x.RelatedWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PhoneNumber", "Status", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "alice@example.com", "Alice Johnson", "555-0101", "Active", null, "alice" },
                    { 2, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "bob@example.com", "Bob Martinez", "555-0102", "Active", null, "bob" },
                    { 3, new DateTime(2024, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "carol@example.com", "Carol White", "555-0103", "Inactive", null, "carol" },
                    { 4, new DateTime(2024, 1, 4, 0, 0, 0, 0, DateTimeKind.Utc), "david@example.com", "David Lee", "555-0104", "Active", null, "david" },
                    { 5, new DateTime(2024, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "emma@example.com", "Emma Davis", "555-0105", "Suspended", null, "emma" }
                });

            migrationBuilder.InsertData(
                table: "Wallets",
                columns: new[] { "Id", "Balance", "CreatedAt", "Currency", "Status", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 1500.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Active", null, 1 },
                    { 2, 320.50m, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Active", null, 2 },
                    { 3, 0.00m, new DateTime(2024, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Frozen", null, 3 },
                    { 4, 9800.75m, new DateTime(2024, 1, 4, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Active", null, 4 },
                    { 5, 50.00m, new DateTime(2024, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "Frozen", null, 5 }
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Amount", "BalanceAfter", "BalanceBefore", "CreatedAt", "Description", "ReferenceNumber", "RelatedWalletId", "Status", "Type", "WalletId" },
                values: new object[,]
                {
                    { 1, 2000m, 2000m, 0m, new DateTime(2024, 1, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Initial deposit", "REF-001", null, "Completed", "Deposit", 1 },
                    { 2, 500m, 1500m, 2000m, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "ATM withdrawal", "REF-002", null, "Completed", "Withdrawal", 1 },
                    { 3, 500m, 500m, 0m, new DateTime(2024, 1, 2, 9, 0, 0, 0, DateTimeKind.Utc), "Bank transfer in", "REF-003", null, "Completed", "Deposit", 2 },
                    { 4, 179.50m, 320.50m, 500m, new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Online purchase", "REF-004", null, "Completed", "Withdrawal", 2 },
                    { 5, 10000m, 10000m, 0m, new DateTime(2024, 1, 4, 9, 0, 0, 0, DateTimeKind.Utc), "Wire transfer", "REF-005", null, "Completed", "Deposit", 4 },
                    { 6, 199.25m, 9800.75m, 10000m, new DateTime(2024, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Monthly fee", "REF-006", null, "Completed", "Fee", 4 },
                    { 7, 100m, 1400m, 1500m, new DateTime(2024, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Transfer to Bob", "REF-007", 2, "Completed", "Transfer", 1 },
                    { 8, 100m, 420.50m, 320.50m, new DateTime(2024, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Transfer from Alice", "REF-008", 1, "Completed", "Transfer", 2 },
                    { 9, 50m, 1450m, 1400m, new DateTime(2024, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Referral bonus", "REF-009", null, "Completed", "Bonus", 1 },
                    { 10, 50m, 1500m, 1450m, new DateTime(2024, 2, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Refund from merchant", "REF-010", null, "Completed", "Refund", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReferenceNumber",
                table: "Transactions",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RelatedWalletId",
                table: "Transactions",
                column: "RelatedWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
