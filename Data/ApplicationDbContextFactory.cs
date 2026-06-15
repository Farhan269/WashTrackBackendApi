using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
                "Server = 192.168.136.53; Database = TestWash; User Id = mis; Password = mis123; TrustServerCertificate = True; MultipleActiveResultSets = true"
           //"Server = UODY-MIS\\SQLEXPRESS;Database=wsahRD; User Id=udoy;Password=udoy; TrustServerCertificate=True;MultipleActiveResultSets=true"
       //"Server = 192.168.11.39\\SQLSERVERDB;Database=wsahRD; User Id=TDS;Password=Sk123; TrustServerCertificate=True;MultipleActiveResultSets=true"
       );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}