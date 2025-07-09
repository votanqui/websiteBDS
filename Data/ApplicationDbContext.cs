using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using thuctap2025.Models;
namespace thuctap2025.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Users> Users { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyCategory> PropertyCategories { get; set; }
        public DbSet<PropertyCategoryMapping> PropertyCategoryMappings { get; set; }
        public DbSet<SeoInfo> SeoInfos { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<PropertyFeature> PropertyFeatures { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<ReportPost> ReportPosts { get; set; }
        public DbSet<ChatMessage> ChatMessage { get; set; }

        public DbSet<PropertyView> PropertyViews { get; set; }
        public DbSet<ContactPageSettings> ContactPageSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NewsImage> NewsImages { get; set; }
        public DbSet<NewsTag> NewsTags { get; set; }
        public DbSet<NewsTagMapping> NewsTagMappings { get; set; }
        public DbSet<NewsCategory> NewsCategories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetToken { get; set; }
        public DbSet<News> News { get; set; }

        public DbSet<SettingHome> SettingHomes { get; set; }

        public DbSet<BankSetting> BankSettings { get; set; }

        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }

        public DbSet<BannedIP> BannedIPs { get; set; }

        public DbSet<NewsView> NewsViews { get; set; }

        public DbSet<UserProfileView> UserProfileViews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(p => p.Token)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(p => new { p.UserId, p.IsUsed });
        }
    }
}
