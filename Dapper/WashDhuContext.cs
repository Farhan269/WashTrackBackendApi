using Microsoft.Data.SqlClient;
using System.Data;

namespace wsahRecieveDelivary.Dapper
{
    public class WashDhuContext
    {
        private readonly IConfiguration _configuration;

        public WashDhuContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("WashDhu"));
        }
    }
}
