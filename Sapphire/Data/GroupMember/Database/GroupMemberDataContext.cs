using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.GroupMember.Database;

public class GroupMemberDataContext : DbContext
{
    public DbSet<GroupMemberAliasDto> GroupMemberAliases { get; set; }
    public DbSet<GroupMemberDto> GroupMembers { get; set; }
    public DbSet<GroupMemberMarriageDto> GroupMemberMarriages { get; set; }
    public DbSet<KnownNameDto> KnownNames { get; set; }
    public DbSet<DisplayNameDto> DisplayNames { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var aliasEntityBuilder = modelBuilder.Entity<GroupMemberAliasDto>();
        aliasEntityBuilder.HasKey(x => x.MemberAliasId);
        aliasEntityBuilder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        aliasEntityBuilder.Property(x => x.SceneUserId).HasMaxLength(64).IsRequired();
        aliasEntityBuilder.Property(x => x.GroupId).HasMaxLength(64).IsRequired();

        var memberEntityBuilder = modelBuilder.Entity<GroupMemberDto>();
        memberEntityBuilder.HasKey(x => x.MemberId);
        memberEntityBuilder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        memberEntityBuilder.Property(x => x.SceneUserId).HasMaxLength(64).IsRequired();
        memberEntityBuilder.Property(x => x.GroupId).HasMaxLength(64).IsRequired();

        var marriageEntityBuilder = modelBuilder.Entity<GroupMemberMarriageDto>();
        marriageEntityBuilder.HasKey(x => x.MarriageId);
        marriageEntityBuilder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        marriageEntityBuilder.Property(x => x.PartnerId).HasMaxLength(64).IsRequired();
        marriageEntityBuilder.Property(x => x.GroupId).HasMaxLength(64).IsRequired();

        var knownNameEntityBuilder = modelBuilder.Entity<KnownNameDto>();
        knownNameEntityBuilder.HasKey(x => x.UserId);
        knownNameEntityBuilder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        knownNameEntityBuilder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();

        var displayNameEntityBuilder = modelBuilder.Entity<DisplayNameDto>();
        displayNameEntityBuilder.HasKey(x => new { x.GroupId, x.UserId });
        displayNameEntityBuilder.Property(x => x.GroupId).HasMaxLength(64).IsRequired();
        displayNameEntityBuilder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        displayNameEntityBuilder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(SpEnvironmentHandler.Environment.Database.SqlConnectionString);
    }
}