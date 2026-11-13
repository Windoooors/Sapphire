using Npgsql;
using NpgsqlTypes;

namespace Sapphire.Migration;

public static class MigrationHandler
{
    public static async Task<MigrationResponse> MigrateAsync(MigrationRequest request, MigrationConfig config,
        CancellationToken cancellationToken)
    {
        if (request.LegacyGroupId is <= 0)
            return new MigrationResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = "旧群号无效"
            };

        await using var legacy = new NpgsqlConnection(config.LegacyDatabase.SqlConnectionString);
        await using var target = new NpgsqlConnection(config.Database.SqlConnectionString);

        await legacy.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        await using var transaction = await target.BeginTransactionAsync(cancellationToken);

        try
        {
            var scores = await CopyScoresAsync(legacy, target, transaction, request, cancellationToken);

            var aliases = request.LegacyGroupId is { } legacyGroupId
                ? await CopyAliasesAsync(legacy, target, transaction, request, legacyGroupId, cancellationToken)
                : 0;

            var members = request.LegacyGroupId.HasValue
                ? await CopyMemberAsync(target, transaction, request, cancellationToken)
                : 0;

            var settings = await CopySettingsAsync(legacy, target, transaction, request, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var count = scores + aliases + members + settings;

            return new MigrationResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Message = count == 0 ? "旧库中没有找到该 QQ 的数据（婚姻未迁移）" : "ok",
                Scores = scores,
                Aliases = aliases,
                Members = members,
                Settings = settings
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new MigrationResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<int> CopyScoresAsync(NpgsqlConnection legacy, NpgsqlConnection target,
        NpgsqlTransaction transaction, MigrationRequest request, CancellationToken cancellationToken)
    {
        await using var read = new NpgsqlCommand(
            "SELECT \"Data\" FROM \"MaiUserPlayRecords\" WHERE \"UserId\" = @qq LIMIT 1", legacy);

        read.Parameters.AddWithValue("qq", request.QqNumber);

        var data = await read.ExecuteScalarAsync(cancellationToken) as byte[];

        if (data == null)
            return 0;

        await using var write = new NpgsqlCommand("""
            INSERT INTO "MaiUserPlayRecords" ("UserId", "Data")
            VALUES (@uid, @data)
            ON CONFLICT ("UserId") DO UPDATE SET "Data" = EXCLUDED."Data"
            """, target, transaction);

        write.Parameters.AddWithValue("uid", request.CanonicalUserId);
        write.Parameters.AddWithValue("data", data);

        await write.ExecuteNonQueryAsync(cancellationToken);

        return 1;
    }

    private static async Task<int> CopyAliasesAsync(NpgsqlConnection legacy, NpgsqlConnection target,
        NpgsqlTransaction transaction, MigrationRequest request, long legacyGroupId,
        CancellationToken cancellationToken)
    {
        const string query = "SELECT \"Alias\" FROM \"GroupMemberAliases\" WHERE \"UserId\" = @qq AND \"GroupId\" = @group";

        await using var read = new NpgsqlCommand(query, legacy);

        read.Parameters.AddWithValue("qq", request.QqNumber);
        read.Parameters.AddWithValue("group", legacyGroupId);

        var aliases = new List<string>();

        await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                if (!reader.IsDBNull(0))
                    aliases.Add(reader.GetString(0));
        }

        if (aliases.Count == 0)
            return 0;

        await using var write = new NpgsqlCommand("""
            INSERT INTO "GroupMemberAliases" ("Alias", "UserId", "SceneUserId", "GroupId")
            SELECT @alias, @uid, @scene, @chat
            WHERE NOT EXISTS (
                SELECT 1 FROM "GroupMemberAliases"
                WHERE "GroupId" = @chat AND "UserId" = @uid AND "Alias" = @alias
            )
            """, target, transaction);

        write.Parameters.Add("alias", NpgsqlDbType.Text);
        write.Parameters.AddWithValue("uid", request.CanonicalUserId);
        write.Parameters.AddWithValue("scene", request.SceneUserId);
        write.Parameters.AddWithValue("chat", request.ChatId);

        var count = 0;

        foreach (var alias in aliases.Distinct(StringComparer.Ordinal))
        {
            write.Parameters["alias"].Value = alias;
            count += await write.ExecuteNonQueryAsync(cancellationToken);
        }

        return count;
    }

    private static async Task<int> CopyMemberAsync(NpgsqlConnection target, NpgsqlTransaction transaction,
        MigrationRequest request, CancellationToken cancellationToken)
    {
        await using var write = new NpgsqlCommand("""
            INSERT INTO "GroupMembers" ("GroupId", "UserId", "SceneUserId")
            SELECT @chat, @uid, @scene
            WHERE NOT EXISTS (
                SELECT 1 FROM "GroupMembers"
                WHERE "GroupId" = @chat AND "UserId" = @uid
            )
            """, target, transaction);

        write.Parameters.AddWithValue("chat", request.ChatId);
        write.Parameters.AddWithValue("uid", request.CanonicalUserId);
        write.Parameters.AddWithValue("scene", request.SceneUserId);

        return await write.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> CopySettingsAsync(NpgsqlConnection legacy, NpgsqlConnection target,
        NpgsqlTransaction transaction, MigrationRequest request, CancellationToken cancellationToken)
    {
        var count = 0;

        count += await CopySettingsAsync(legacy, target, transaction, request, false, request.QqNumber,
            request.CanonicalUserId, cancellationToken);

        if (request.LegacyGroupId is { } legacyGroupId)
            count += await CopySettingsAsync(legacy, target, transaction, request, true,
                legacyGroupId, request.ChatId, cancellationToken);

        return count;
    }

    private static async Task<int> CopySettingsAsync(NpgsqlConnection legacy, NpgsqlConnection target,
        NpgsqlTransaction transaction, MigrationRequest request, bool isGroup, long legacyChatId,
        string targetChatId, CancellationToken cancellationToken)
    {
        await using var read = new NpgsqlCommand("""
            SELECT "Identifier", "Value"
            FROM "SettingsItems"
            WHERE "IsGroup" = @isGroup AND "ChatId" = @chat
            """, legacy);

        read.Parameters.AddWithValue("isGroup", isGroup);
        read.Parameters.AddWithValue("chat", legacyChatId);

        var settings = new List<(string Identifier, bool Value)>();

        await using (var reader = await read.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                if (!reader.IsDBNull(0))
                    settings.Add((reader.GetString(0), reader.GetBoolean(1)));
        }

        if (settings.Count == 0)
            return 0;

        await using var write = new NpgsqlCommand("""
            INSERT INTO "SettingsItems" ("IsGroup", "ChatId", "Identifier", "Value")
            VALUES (@isGroup, @chat, @identifier, @value)
            ON CONFLICT ("IsGroup", "ChatId", "Identifier")
            DO UPDATE SET "Value" = EXCLUDED."Value"
            """, target, transaction);

        write.Parameters.AddWithValue("isGroup", isGroup);
        write.Parameters.AddWithValue("chat", targetChatId);
        write.Parameters.Add("identifier", NpgsqlDbType.Text);
        write.Parameters.Add("value", NpgsqlDbType.Boolean);

        var count = 0;

        foreach (var setting in settings)
        {
            write.Parameters["identifier"].Value = setting.Identifier;
            write.Parameters["value"].Value = setting.Value;
            count += await write.ExecuteNonQueryAsync(cancellationToken);
        }

        return count;
    }
}
