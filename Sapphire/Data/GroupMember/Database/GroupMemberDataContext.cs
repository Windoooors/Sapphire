using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.GroupMember.Database;

public class GroupMemberDataContext : DbContext
{
    public DbSet<GroupMemberAliasDto> GroupMemberAliases { get; set; }
    public DbSet<GroupMemberDto> GroupMembers { get; set; }
    public DbSet<GroupMemberMarriageDto> GroupMemberMarriages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var aliasEntityBuilder = modelBuilder.Entity<GroupMemberAliasDto>();
        aliasEntityBuilder.HasKey(x => x.MemberAliasId);

        var memberEntityBuilder = modelBuilder.Entity<GroupMemberDto>();
        memberEntityBuilder.HasKey(x => x.MemberId);

        var marriageEntityBuilder = modelBuilder.Entity<GroupMemberMarriageDto>();
        marriageEntityBuilder.HasKey(x => x.MarriageId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Environment.Database.SqlConnectionString);
    }
}