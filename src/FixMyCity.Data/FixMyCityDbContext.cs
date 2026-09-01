using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data.Models;

namespace FixMyCity.Data
{
    public class FixMyCityDbContext : IdentityDbContext<ApplicationUser>
    {
        public FixMyCityDbContext(DbContextOptions<FixMyCityDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<Issue> Issues { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<Evidence> EvidenceItems { get; set; } = null!;
        public DbSet<StatusHistory> StatusHistories { get; set; } = null!;
    }
}