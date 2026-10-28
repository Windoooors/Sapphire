using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.GroupMember.Database;

public class GroupMemberDataContext : DbContext
{
    public DbSet<GroupMemberAlias> GroupMemberAliases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var aliasEntityBuilder = modelBuilder.Entity<GroupMemberAlias>();
        aliasEntityBuilder.HasKey(x => x.MemberAliasId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Environment.Database.SqlConnectionString);
    }
}