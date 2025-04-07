using Microsoft.EntityFrameworkCore;
using Shared.Models; // Thay YourProject bằng tên không gian tên của bạn

namespace Server.Data // Đảm bảo namespace này chính xác
{
    public class AppDbContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Effect> Effects { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Cultivation> Cultivations { get; set; }
        public DbSet<Weapon> Weapons { get; set; }
        public DbSet<Armor> Armors { get; set; }
        public DbSet<User> Users { get; set; } 

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Card>()
                .HasOne(c => c.Effect)
                .WithMany()
                .HasForeignKey(c => c.EffectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình khác nếu cần
        }
    }
}
