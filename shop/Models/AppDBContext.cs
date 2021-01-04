using Microsoft.EntityFrameworkCore;

namespace shop.Models
{
    public class AppDBContext:DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> opt) : base(opt)
        {

        }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<DailyReport> DailyReports { get; set; }
        public DbSet<FailedReservation> FailedReservations { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Reservation>()
                .HasKey(r => new { r.id, r.clientPersonalCode });
            builder.Entity<DailyReport>()
                .HasKey(dr => new { dr.date });
        }
    }
}
