using Microsoft.Data.SqlClient;
using System.Data;

namespace wsahRecieveDelivary.Dapper
{
    public class WashDhuTWLContext
    {
        private readonly IConfiguration _configuration;

        public WashDhuTWLContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("WashDhuTWL"));
        }
    }
}
