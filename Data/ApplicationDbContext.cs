using DRB_TEMP.Models;
using Microsoft.EntityFrameworkCore;

namespace DRB_TEMP.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TemperatureDailyLog> TemperatureDailyLogs { get; set; }

        public DbSet<TemperatureIntradayLog> TemperatureIntradayLogs { get; set; }
    }
}