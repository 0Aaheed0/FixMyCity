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

        // Smart Municipal Portal Additions
        public DbSet<MunicipalService> MunicipalServices { get; set; } = null!;
        public DbSet<MunicipalProvider> MunicipalProviders { get; set; } = null!;
        public DbSet<CitizenBill> CitizenBills { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<LostFoundPost> LostFoundPosts { get; set; } = null!;
        public DbSet<LostFoundResponse> LostFoundResponses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Indexes for fast lookup
            builder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            builder.Entity<Announcement>()
                .HasIndex(a => new { a.Status, a.StartDate, a.ExpiryDate });

            builder.Entity<EmergencyAlert>()
                .HasIndex(e => new { e.Status, e.StartAt, e.ExpireAt });

            builder.Entity<CitizenBill>()
                .HasIndex(b => new { b.UserId, b.Status });

            builder.Entity<CitizenBill>()
                .HasIndex(b => new { b.ProviderId, b.ConsumerNumber });

            builder.Entity<LostFoundPost>()
                .HasIndex(l => new { l.Status, l.PostType, l.CreatedAt });

            // Relationships
            builder.Entity<LostFoundResponse>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Responses)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}