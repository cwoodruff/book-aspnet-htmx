using ChinookDashboard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Data;

public class ChinookContext(DbContextOptions<ChinookContext> options) : DbContext(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Artist configuration
        modelBuilder.Entity<Artist>(entity =>
        {
            entity.ToTable("Artist");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(120);
        });

        // Album configuration
        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("Album");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Title).HasColumnName("Title").HasMaxLength(160);
            entity.Property(e => e.ArtistId).HasColumnName("ArtistId");
            
            entity.HasOne(d => d.Artist)
                .WithMany(p => p.Albums)
                .HasForeignKey(d => d.ArtistId);
        });

        // Track configuration
        modelBuilder.Entity<Track>(entity =>
        {
            entity.ToTable("Track");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(200);
            entity.Property(e => e.AlbumId).HasColumnName("AlbumId");
            entity.Property(e => e.GenreId).HasColumnName("GenreId");
            entity.Property(e => e.Composer).HasColumnName("Composer").HasMaxLength(220);
            entity.Property(e => e.Milliseconds).HasColumnName("Milliseconds");
            entity.Property(e => e.Bytes).HasColumnName("Bytes");
            entity.Property(e => e.UnitPrice).HasColumnName("UnitPrice").HasColumnType("NUMERIC(10,2)");
            
            entity.HasOne(d => d.Album)
                .WithMany(p => p.Tracks)
                .HasForeignKey(d => d.AlbumId);
                
            entity.HasOne(d => d.Genre)
                .WithMany(p => p.Tracks)
                .HasForeignKey(d => d.GenreId);
        });

        // Genre configuration
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genre");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(120);
        });
    }
}
