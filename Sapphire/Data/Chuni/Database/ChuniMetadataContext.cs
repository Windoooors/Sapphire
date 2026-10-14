using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Chuni.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.Chuni.Database;

public class ChuniMetadataContext : DbContext
{
    public DbSet<ChuniSongDto> ChuniSongs { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Environment.Database.SqlConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var songEntityBuilder = modelBuilder.Entity<ChuniSongDto>();

        songEntityBuilder
            .Property(s => s.SongId)
            .ValueGeneratedNever();

        songEntityBuilder.HasKey(x => x.SapphireId);

        var chartEntityBuilder = modelBuilder.Entity<ChuniChartDto>();
        chartEntityBuilder
            .HasOne(a => a.Song)
            .WithMany(m => m.Charts)
            .HasForeignKey(a => a.SapphireId);

        chartEntityBuilder.HasKey(x => x.ChartId);
    }
}