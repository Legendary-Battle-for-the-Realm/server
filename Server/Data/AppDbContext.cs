using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Effect> Effects { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<EquipmentSkill> EquipmentSkills { get; set; }
        public DbSet<Cultivation> Cultivations { get; set; }
        public DbSet<Weapon> Weapons { get; set; }
        public DbSet<Armor> Armors { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<CardLocation> CardLocations { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình quan hệ giữa Character và Skill
            modelBuilder.Entity<Character>()
                .HasMany(c => c.Skills)
                .WithOne(s => s.Character)
                .HasForeignKey(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ giữa Armor và EquipmentSkill
            modelBuilder.Entity<Armor>()
                .HasOne(a => a.Skill)
                .WithMany()
                .HasForeignKey(a => a.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ giữa Weapon và EquipmentSkill
            modelBuilder.Entity<Weapon>()
                .HasOne(w => w.Skill)
                .WithMany()
                .HasForeignKey(w => w.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ giữa Card và Effect
            modelBuilder.Entity<Card>()
                .HasOne(c => c.Effect)
                .WithMany()
                .HasForeignKey(c => c.EffectId)
                .IsRequired(false);

            // Cấu hình mối quan hệ giữa CardLocation và Card
            modelBuilder.Entity<CardLocation>()
                .HasOne(cl => cl.Card)
                .WithMany()
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ giữa CardLocation và Room
            modelBuilder.Entity<CardLocation>()
                .HasOne(cl => cl.Room)
                .WithMany()
                .HasForeignKey(cl => cl.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình mối quan hệ giữa CardLocation và Player
            modelBuilder.Entity<CardLocation>()
                .HasOne(cl => cl.Player)
                .WithMany()
                .HasForeignKey(cl => cl.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình mối quan hệ giữa Room và Player
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Players)
                .WithOne(p => p.Room)
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}