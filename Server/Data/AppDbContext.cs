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
                .WithOne()
                .HasForeignKey<Armor>(a => a.SkillId);

            // Cấu hình quan hệ giữa Weapon và EquipmentSkill
            modelBuilder.Entity<Weapon>()
                .HasOne(w => w.Skill)
                .WithOne()
                .HasForeignKey<Weapon>(w => w.SkillId);

            // Cấu hình các thuộc tính bắt buộc cho Character
            modelBuilder.Entity<Character>()
                .Property(c => c.Name)
                .IsRequired();

            modelBuilder.Entity<Character>()
                .Property(c => c.Desc)
                .IsRequired();

            modelBuilder.Entity<Character>()
                .Property(c => c.Faction)
                .IsRequired();

            modelBuilder.Entity<Character>()
                .Property(c => c.Cultivation)
                .IsRequired();

            // Cấu hình các thuộc tính bắt buộc cho Skill
            modelBuilder.Entity<Skill>()
                .Property(s => s.Name)
                .IsRequired();

            modelBuilder.Entity<Skill>()
                .Property(s => s.Ref)
                .IsRequired();

            modelBuilder.Entity<Skill>()
                .Property(s => s.Desc)
                .IsRequired(false);

            modelBuilder.Entity<Skill>()
                .Property(s => s.Effect)
                .IsRequired(false);

            // Cấu hình các thuộc tính bắt buộc cho EquipmentSkill
            modelBuilder.Entity<EquipmentSkill>()
                .Property(es => es.Name)
                .IsRequired();

            modelBuilder.Entity<EquipmentSkill>()
                .Property(es => es.Ref)
                .IsRequired();

            // Cấu hình các thuộc tính bắt buộc cho Card
            modelBuilder.Entity<Card>()
                .Property(c => c.Name)
                .IsRequired();

            modelBuilder.Entity<Card>()
                .Property(c => c.Desc)
                .IsRequired();

            modelBuilder.Entity<Card>()
                .Property(c => c.Ref)
                .IsRequired();

            modelBuilder.Entity<Card>()
                .Property(c => c.Type)
                .IsRequired();

            modelBuilder.Entity<Card>()
                .Property(c => c.Quantity)
                .IsRequired();

            // Cấu hình quan hệ giữa Card và Effect
            modelBuilder.Entity<Card>()
                .HasOne(c => c.Effect)
                .WithMany()
                .HasForeignKey(c => c.EffectId)
                .IsRequired(false);

            // Cấu hình các thuộc tính bắt buộc cho Effect
            modelBuilder.Entity<Effect>()
                .Property(e => e.Name)
                .IsRequired();

            modelBuilder.Entity<Effect>()
                .Property(e => e.Ref)
                .IsRequired();

            // Cấu hình các thuộc tính bắt buộc cho Cultivation
            modelBuilder.Entity<Cultivation>()
                .Property(c => c.Name)
                .IsRequired();

            modelBuilder.Entity<Cultivation>()
                .Property(c => c.RequiredQi)
                .IsRequired();

            // Cấu hình các thuộc tính bắt buộc cho Armor
            modelBuilder.Entity<Armor>()
                .Property(a => a.Name)
                .IsRequired();

            modelBuilder.Entity<Armor>()
                .Property(a => a.Atk)
                .IsRequired();

            modelBuilder.Entity<Armor>()
                .Property(a => a.Def)
                .IsRequired();

            modelBuilder.Entity<Armor>()
                .Property(a => a.Desc)
                .IsRequired();

            modelBuilder.Entity<Armor>()
                .Property(a => a.UserId)
                .IsRequired();

            modelBuilder.Entity<Armor>()
                .Property(a => a.CultivationRequired)
                .IsRequired();

            // Cấu hình các thuộc tính bắt buộc cho Weapon
            modelBuilder.Entity<Weapon>()
                .Property(w => w.Name)
                .IsRequired();

            modelBuilder.Entity<Weapon>()
                .Property(w => w.Atk)
                .IsRequired();

            modelBuilder.Entity<Weapon>()
                .Property(w => w.Desc)
                .IsRequired();

            modelBuilder.Entity<Weapon>()
                .Property(w => w.UserId)
                .IsRequired();

            modelBuilder.Entity<Weapon>()
                .Property(w => w.CultivationRequired)
                .IsRequired();

            modelBuilder.Entity<Room>()
                .HasMany(r => r.Players)
                .WithOne()
                .HasForeignKey(p => p.RoomId);

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