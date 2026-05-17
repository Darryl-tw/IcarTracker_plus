using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TrackerPlus.Core.Common;

namespace TrackerPlus.Repository.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateMainConnection();
    SqlConnection CreateLogConnection();
    SqlConnection CreateCellularBaseConnection();
    SqlConnection CreateChinaOffsetConnection();
    SqlConnection CreateAddressConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseSettings _settings;

    public DbConnectionFactory(IOptions<DatabaseSettings> settings)
    {
        _settings = settings.Value;
    }

    public SqlConnection CreateMainConnection()
        => new SqlConnection(_settings.MainConnection);

    public SqlConnection CreateLogConnection()
        => new SqlConnection(_settings.LogConnection);

    public SqlConnection CreateCellularBaseConnection()
        => new SqlConnection(_settings.CellularBaseConnection);

    public SqlConnection CreateChinaOffsetConnection()
        => new SqlConnection(_settings.ChinaOffsetConnection);

    public SqlConnection CreateAddressConnection()
        => new SqlConnection(_settings.AddressConnection);
}
