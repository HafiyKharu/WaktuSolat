using Microsoft.EntityFrameworkCore;
using WaktuSolat.Models;

namespace WaktuSolat.Repository;

public class WaktuSolatDbContext : DbContext
{
    public WaktuSolatDbContext(DbContextOptions<WaktuSolatDbContext> options) : base(options)
    {
    }

    public DbSet<WaktuSolatEntity> WaktuSolat { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WaktuSolatEntity>(entity =>
        {
            entity.ToTable("waktu_solat");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.czone)
                .HasColumnName("czone")
                .HasMaxLength(100); // Increased from 10 to 100
            entity.Property(e => e.cbearing)
                .HasColumnName("cbearing")
                .HasMaxLength(100); // Increased from 50 to 100
            entity.Property(e => e.TarikhMasehi)
                .HasColumnName("tarikh_masehi")
                .HasMaxLength(20);
            entity.Property(e => e.TarikhHijrah)
                .HasColumnName("tarikh_hijrah")
                .HasMaxLength(20);
            entity.Property(e => e.Imsak)
                .HasColumnName("imsak")
                .HasMaxLength(15); // Increased from 10 to 15 for time format
            entity.Property(e => e.Subuh)
                .HasColumnName("subuh")
                .HasMaxLength(15);
            entity.Property(e => e.Syuruk)
                .HasColumnName("syuruk")
                .HasMaxLength(15);
            entity.Property(e => e.Dhuha)
                .HasColumnName("dhuha")
                .HasMaxLength(15);
            entity.Property(e => e.Zohor)
                .HasColumnName("zohor")
                .HasMaxLength(15);
            entity.Property(e => e.Asar)
                .HasColumnName("asar")
                .HasMaxLength(15);
            entity.Property(e => e.Maghrib)
                .HasColumnName("maghrib")
                .HasMaxLength(15);
            entity.Property(e => e.Isyak)
                .HasColumnName("isyak")
                .HasMaxLength(15);
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        base.OnModelCreating(modelBuilder);
    }
}