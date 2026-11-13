using Npgsql;
using Sapphire.Environments;

namespace Sapphire.Data.Migration;

/// <summary>
///     记录某个 canonical 用户是否已经完成旧库迁移。表由本类按需创建，避免再引入 EF 迁移。
/// </summary>
public static class UserMigrationStatusManager
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static bool _initialized;

    public static async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();

        try
        {
            if (_initialized)
                return;

            await using var connection = new NpgsqlConnection(
                SpEnvironmentHandler.Environment.Database.SqlConnectionString);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("""
                CREATE TABLE IF NOT EXISTS "UserMigrationStatus" (
                    "UserId" character varying(64) NOT NULL,
                    "QqNumber" bigint NOT NULL,
                    "MigratedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_UserMigrationStatus" PRIMARY KEY ("UserId")
                );

                INSERT INTO "UserMigrationStatus" ("UserId", "QqNumber", "MigratedAt")
                SELECT DISTINCT "UserId", 0, now() FROM "MaiUserPlayRecords"
                ON CONFLICT ("UserId") DO NOTHING;

                INSERT INTO "UserMigrationStatus" ("UserId", "QqNumber", "MigratedAt")
                SELECT DISTINCT "UserId", 0, now() FROM "GroupMemberAliases"
                ON CONFLICT ("UserId") DO NOTHING;
                """, connection);

            await command.ExecuteNonQueryAsync();
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public static async Task<bool> HasMigratedAsync(string userId)
    {
        await EnsureInitializedAsync();

        await using var connection = new NpgsqlConnection(
            SpEnvironmentHandler.Environment.Database.SqlConnectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT 1 FROM \"UserMigrationStatus\" WHERE \"UserId\" = @userId LIMIT 1", connection);

        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteScalarAsync() != null;
    }

    public static async Task MarkMigratedAsync(string userId, long qqNumber)
    {
        await EnsureInitializedAsync();

        await using var connection = new NpgsqlConnection(
            SpEnvironmentHandler.Environment.Database.SqlConnectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("""
            INSERT INTO "UserMigrationStatus" ("UserId", "QqNumber", "MigratedAt")
            VALUES (@userId, @qqNumber, now())
            ON CONFLICT ("UserId") DO UPDATE
            SET "QqNumber" = EXCLUDED."QqNumber", "MigratedAt" = EXCLUDED."MigratedAt"
            """, connection);

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("qqNumber", qqNumber);

        await command.ExecuteNonQueryAsync();
    }
}
