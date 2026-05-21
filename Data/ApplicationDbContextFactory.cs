using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace wsahRecieveDelivary.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
           "Server = UODY-MIS\\SQLEXPRESS;Database=wsahRD; User Id=udoy;Password=udoy; TrustServerCertificate=True;MultipleActiveResultSets=true"
       //"Server = 192.168.11.39\\SQLSERVERDB;Database=wsahRD; User Id=TDS;Password=Sk123; TrustServerCertificate=True;MultipleActiveResultSets=true"
       );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}