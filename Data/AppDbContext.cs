using Microsoft.EntityFrameworkCore;

namespace ClassTrack.Data
{
    public class AppDbContext : DbContext
    {
        // Same database the ClassTrack.Web app created. Change the server name here if you
        // use a different SQL Server instance.
        public const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=ClassTrackDb;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=true";

        public DbSet<Student> Students => Set<Student>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }
    }
}
