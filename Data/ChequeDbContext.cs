using AmarTools.Bankcheck.Models;
using Microsoft.EntityFrameworkCore;

namespace AmarTools.Bankcheck.Data;

public class ChequeDbContext : DbContext
{
    public ChequeDbContext(DbContextOptions<ChequeDbContext> options) : base(options) { }

    public DbSet<ChequeTemplate> ChequeTemplates { get; set; } = null!;
    public DbSet<ChequeLayout> ChequeLayouts { get; set; } = null!;
    public DbSet<PrintedCheque> PrintedCheques { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChequeTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.BankName).IsRequired().HasMaxLength(100);

            e.HasMany(t => t.Layouts)
             .WithOne(l => l.ChequeTemplate)
             .HasForeignKey(l => l.ChequeTemplateId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(t => t.PrintedCheques)
            .WithOne(p => p.ChequeTemplate)
            .HasForeignKey(p => p.ChequeTemplateId)
            .OnDelete(DeleteBehavior.Cascade);     // ← Changed to Cascade
        });

        modelBuilder.Entity<ChequeLayout>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.FieldName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<PrintedCheque>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");

            e.HasOne(p => p.ChequeTemplate)
             .WithMany(t => t.PrintedCheques)
             .HasForeignKey(p => p.ChequeTemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        modelBuilder.Entity<ChequeTemplate>().HasData(new ChequeTemplate
        {
            Id = 1,
            BankName = "City Bank",
            AccountNumber = "1234567890",
            PageWidth = 842f,
            PageHeight = 300f,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<ChequeLayout>().HasData(
            new ChequeLayout
            {
                Id = 1,
                ChequeTemplateId = 1,
                FieldName = "Payee",
                X = 140f,
                Y = 185f,
                FontSize = 11f,
                FontStyle = "Normal",
                FontColor = "#000000",
                MaxLength = 100
            },
            new ChequeLayout
            {
                Id = 2,
                ChequeTemplateId = 1,
                FieldName = "Amount",
                X = 660f,
                Y = 185f,
                FontSize = 11f,
                FontStyle = "Bold",
                FontColor = "#000000",
                MaxLength = 20
            },
            new ChequeLayout
            {
                Id = 3,
                ChequeTemplateId = 1,
                FieldName = "AmountWords",
                X = 90f,
                Y = 155f,
                FontSize = 10f,
                FontStyle = "Normal",
                FontColor = "#000000",
                MaxLength = 200
            },
            new ChequeLayout
            {
                Id = 4,
                ChequeTemplateId = 1,
                FieldName = "Date",
                X = 660f,
                Y = 240f,
                FontSize = 10f,
                FontStyle = "Normal",
                FontColor = "#000000",
                MaxLength = 20
            },
            new ChequeLayout
            {
                Id = 5,
                ChequeTemplateId = 1,
                FieldName = "ChequeNo",
                X = 90f,
                Y = 55f,
                FontSize = 9f,
                FontStyle = "Normal",
                FontColor = "#555555",
                MaxLength = 30
            }
        );
    }
}