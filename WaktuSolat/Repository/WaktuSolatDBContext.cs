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
            entity.ToTable("WaktuSolat");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.czone).HasMaxLength(10);
            entity.Property(e => e.cbearing).HasMaxLength(50);
            entity.Property(e => e.TarikhMasehi).HasMaxLength(20);
            entity.Property(e => e.TarikhHijrah).HasMaxLength(20);
            entity.Property(e => e.Imsak).HasMaxLength(10);
            entity.Property(e => e.Subuh).HasMaxLength(10);
            entity.Property(e => e.Syuruk).HasMaxLength(10);
            entity.Property(e => e.Dhuha).HasMaxLength(10);
            entity.Property(e => e.Zohor).HasMaxLength(10);
            entity.Property(e => e.Asar).HasMaxLength(10);
            entity.Property(e => e.Maghrib).HasMaxLength(10);
            entity.Property(e => e.Isyak).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        base.OnModelCreating(modelBuilder);
    }
}