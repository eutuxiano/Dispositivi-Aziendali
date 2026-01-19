using Microsoft.EntityFrameworkCore;
using DeviceManagerMVC.Models;

namespace DeviceManagerMVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // 👉 Aggiungi questo costruttore vuoto per le migrazioni
        public AppDbContext() { }

        public DbSet<Device> Devices { get; set; }
    }
}
