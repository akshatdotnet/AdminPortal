using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace PMS.Infrastructure.Dapper;

/// <summary>
/// Provides raw SQL connections for Dapper read queries.
/// Lightweight — no change tracking overhead.
/// </summary>
public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}