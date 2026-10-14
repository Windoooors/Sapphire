using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.Mai.Database;

public class MaiUserDataContext : DbContext
{
    public DbSet<MaiUserPlayRecordDto> MaiUserPlayRecords { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Environment.Database.SqlConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userRecordEntityBuilder = modelBuilder.Entity<MaiUserPlayRecordDto>();
        userRecordEntityBuilder.HasKey(x => x.UserId);
        userRecordEntityBuilder.Property(x => x.UserId).ValueGeneratedNever();
        userRecordEntityBuilder.Property(x => x.Data).HasColumnType("bytea").IsRequired();
    }
}