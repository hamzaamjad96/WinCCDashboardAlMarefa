using Microsoft.Data.SqlClient;

namespace WinCCDashboard.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateAsync(
        CancellationToken cancellationToken = default);
}