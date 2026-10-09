using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Chuni.Dtos;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.Mai.Database;

public class MaiMetadataContext : DbContext
{
    public DbSet<MaiSongDto> MaiSongs { get; init; }
    public DbSet<MaiAliasDto> MaiAliases { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Instance?.Environment.Database.SqlConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var songEntityBuilder = modelBuilder.Entity<MaiSongDto>();

        songEntityBuilder
            .Property(s => s.SongId)
            .ValueGeneratedNever();

        songEntityBuilder.HasKey(x => x.SapphireId);

        var chartEntityBuilder = modelBuilder.Entity<MaiChartDto>();
        chartEntityBuilder
            .HasOne(a => a.Song)
            .WithMany(m => m.Charts)
            .HasForeignKey(a => a.SapphireId);

        chartEntityBuilder.HasKey(x => x.ChartId);
        
        var aliasEntityBuilder = modelBuilder.Entity<MaiAliasDto>();
        aliasEntityBuilder.HasOne(x => x.Song);
        
        aliasEntityBuilder.HasKey(x => x.AliasId);
    }
}