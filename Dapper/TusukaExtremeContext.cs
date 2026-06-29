using Microsoft.Data.SqlClient;
using System.Data;

namespace wsahRecieveDelivary.Dapper
{
    public class TusukaExtremeContext
    {
        private readonly IConfiguration _configuration;

        public TusukaExtremeContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("TusukaExtreme"));


        }
    }
}
