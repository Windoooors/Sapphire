using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Data.Settings.Dtos;
using Sapphire.Environments;
using Sapphire.UserSettings;

namespace Sapphire.Data.Settings.Database;

public class SettingsMetadataContext : DbContext
{
    public DbSet<SettingsStorageItemDto> SettingsItems { get; init; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Instance?.Environment.Database.SqlConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var itemEntityBuilder = modelBuilder.Entity<SettingsStorageItemDto>();

        itemEntityBuilder
            .Property(s => s.ChatId)
            .ValueGeneratedNever();

        itemEntityBuilder.HasKey(x => new
        {
            x.IsGroup, x.ChatId, x.Identifier
        });

        itemEntityBuilder.Property(x => x.ChatId).IsRequired();
        itemEntityBuilder.Property(x => x.IsGroup).IsRequired();
        itemEntityBuilder.Property(x => x.Identifier).IsRequired();
    }
}