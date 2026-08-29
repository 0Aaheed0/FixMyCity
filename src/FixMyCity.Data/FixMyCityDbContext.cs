using Microsoft.EntityFrameworkCore;
using FixMyCity.Data.Models;

namespace FixMyCity.Data
{
    public class FixMyCityDbContext : DbContext
    {
        public FixMyCityDbContext(DbContextOptions<FixMyCityDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<Issue> Issues { get; set; } = null!;
    }
}