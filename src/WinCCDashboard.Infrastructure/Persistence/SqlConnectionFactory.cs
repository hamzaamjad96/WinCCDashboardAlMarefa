using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace WinCCDashboard.Infrastructure.Persistence;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("ReportingDatabase")
            ?? throw new InvalidOperationException(
                "ReportingDatabase connection string not found.");
    }

    public async Task<SqlConnection> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}