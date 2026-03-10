using Microsoft.EntityFrameworkCore;
using WebReport.Models.Entities;

namespace WebReport.Models
{
    public class WebReportDBContext : DbContext
    {
        public WebReportDBContext(DbContextOptions<WebReportDBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships if needed
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(u => u.Users);
        }
    }
}
