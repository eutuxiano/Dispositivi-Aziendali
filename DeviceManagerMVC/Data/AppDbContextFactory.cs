using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DeviceManagerMVC.Models;

namespace DeviceManagerMVC.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DeviceManagerMVC;Trusted_Connection=True;");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
