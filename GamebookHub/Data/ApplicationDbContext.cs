using GamebookHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GamebookHub.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSets existentes
        public DbSet<Gamebook> Gamebooks => Set<Gamebook>();
        public DbSet<GameNode> GameNodes => Set<GameNode>();
        public DbSet<GameChoice> GameChoices => Set<GameChoice>();
        public DbSet<Playthrough> Playthroughs => Set<Playthrough>();

        // Novos DbSets (ficha genérica)
        public DbSet<CharacterSheetTemplate> CharacterSheets => Set<CharacterSheetTemplate>();
        public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
        // InventoryConfig é OWNED (não precisa DbSet)

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ----------------- Regras já existentes -----------------
            b.Entity<Gamebook>()
                .HasIndex(g => g.Slug)
                .IsUnique();

            b.Entity<GameNode>()
                .HasIndex(n => new { n.GamebookId, n.Key })
                .IsUnique();

            b.Entity<GameChoice>()
                .HasOne(c => c.FromNode)
                .WithMany(n => n.Choices)
                .HasForeignKey(c => c.FromNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Playthrough>()
                .HasIndex(p => new { p.UserId, p.GamebookId })
                .IsUnique();

            // ----------------- NOVO: Gamebook ↔ CharacterSheet (1:1 opcional) -----------------
            // FK sombra em Gamebook: CharacterSheetId
            b.Entity<Gamebook>()
                .Property<int?>("CharacterSheetId");

            b.Entity<Gamebook>()
                .HasOne(g => g.CharacterSheet)
                .WithOne()
                .HasForeignKey<Gamebook>("CharacterSheetId")
                .OnDelete(DeleteBehavior.SetNull);

            // ----------------- CharacterSheetTemplate -----------------
            b.Entity<CharacterSheetTemplate>(cs =>
            {
                cs.Property(p => p.Enabled).IsRequired();

                // Inventory como Owned Type (colunas na mesma tabela)
                cs.OwnsOne(p => p.Inventory, inv =>
                {
                    inv.Property(i => i.Enabled).IsRequired();
                    inv.Property(i => i.Mode)
                        .HasConversion<string>()
                        .HasMaxLength(16);
                    inv.Property(i => i.Slots);
                    inv.Property(i => i.Capacity).HasColumnType("decimal(18,2)");
                });

                // 1:N com AttributeDefinition
                cs.HasMany(p => p.Attributes)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Cascade);
            });

            // ----------------- AttributeDefinition -----------------
            b.Entity<AttributeDefinition>(ad =>
            {
                ad.Property(p => p.Key)
                  .IsRequired()
                  .HasMaxLength(64);
                ad.Property(p => p.Label)
                  .IsRequired()
                  .HasMaxLength(64);

                ad.Property(p => p.Type)
                  .HasConversion<string>()
                  .HasMaxLength(16);

                ad.Property(p => p.Min);
                ad.Property(p => p.Max);
                ad.Property(p => p.Default);
                ad.Property(p => p.Visible).HasDefaultValue(true);
                ad.Property(p => p.Order).HasDefaultValue(0);
                ad.Property(p => p.EnumOptions).HasMaxLength(512);

                // Unicidade da Key por ficha (FK por convenção: CharacterSheetTemplateId)
                ad.HasIndex("CharacterSheetTemplateId", nameof(AttributeDefinition.Key))
                  .IsUnique();
            });
        }
    }
}
