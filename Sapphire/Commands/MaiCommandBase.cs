using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands.MaiCommands;
using Sapphire.Data.DivingFishMai;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Commands;

public abstract class MaiCommandBase : CommandBase
{
    protected static MaiDataManager? DataManager;
    protected static DivingFishMaiDataManager? DivingFishMaiDataManager;

    protected static bool TryGetSongsByString(
        string key,
        out MaiSongDto[]? songs)
    {
        songs = null;

        var manager = MaiDataManager.Instance;

        if (manager == null)
            return false;

        if (string.IsNullOrWhiteSpace(key))
        {
            songs = Array.Empty<MaiSongDto>();
            return true;
        }

        var loweredKey = key.ToLowerInvariant();

        var searchSid = loweredKey.StartsWith(
            "sid", StringComparison.Ordinal);

        var searchId = !searchSid && loweredKey.StartsWith(
            "id", StringComparison.Ordinal);

        if (searchId || searchSid)
        {
            var idString = loweredKey[
                (searchSid ? 3 : 2)..].Trim();

            if (int.TryParse(idString, out var id))
            {
                var result = new List<MaiSongDto>();

                if (searchId &&
                    manager.IdToSong.TryGetValue(id, out var idSong))
                    result.Add(idSong);

                if (searchSid &&
                    manager.SapphireIdToSong.TryGetValue(id, out var sidSong))
                    result.Add(sidSong);

                songs = result.Distinct().ToArray();
                return true;
            }
        }

        // 精确匹配
        List<MaiSongDto> MatchExact(string searchKey)
        {
            var result = new List<MaiSongDto>();

            if (manager.SongNameToSongs.TryGetValue(
                    searchKey, out var nameSongs))
                result.AddRange(nameSongs);

            if (manager.AliasToSongs.TryGetValue(
                    searchKey, out var aliasSongs))
                result.AddRange(aliasSongs);

            return result.Distinct().ToList();
        }

        // 模糊匹配

        List<MaiSongDto> BlurredMatch(
            string searchKey,
            IEnumerable<MaiSongDto> candidates,
            char? utagePrefix = null)
        {
            Func<MaiSongDto, bool> namePredicate;
            Func<MaiSongDto, bool> aliasPredicate;

            if (utagePrefix.HasValue)
            {
                namePredicate =
                    SearchCommand.CreateSongNamePredicate(
                        searchKey, utagePrefix.Value);

                aliasPredicate =
                    SearchCommand.CreateSongAliasPredicate(
                        searchKey, utagePrefix.Value);
            }
            else
            {
                namePredicate =
                    SearchCommand.CreateSongNamePredicate(searchKey);

                aliasPredicate =
                    SearchCommand.CreateSongAliasPredicate(searchKey);
            }

            return candidates
                .Where(x => namePredicate(x) || aliasPredicate(x))
                .Distinct()
                .ToList();
        }

        // 种类分类查询

        var searchDx = loweredKey.StartsWith(
            "dx", StringComparison.Ordinal);

        var searchStd = loweredKey.StartsWith(
            "标", StringComparison.Ordinal);

        if (searchDx || searchStd)
        {
            var searchKey = loweredKey[
                (searchDx ? 2 : 1)..].Trim();

            var targetType = searchDx
                ? MaiSongType.Dx
                : MaiSongType.Std;

            var result = MatchExact(searchKey)
                .Where(x => x.SongType == targetType)
                .ToList();

            if (result.Count == 0)
                result = BlurredMatch(
                        searchKey,
                        manager.Songs)
                    .Where(x => x.SongType == targetType)
                    .ToList();

            songs = result.ToArray();
            return true;
        }

        // 普通歌曲匹配
        var regularSongs = manager.Songs.Where(x => x.SongType != MaiSongType.Utage);

        var regularResult = MatchExact(loweredKey)
            .Where(x => x.SongType != MaiSongType.Utage)
            .ToList();

        if (regularResult.Count > 0)
        {
            songs = regularResult.ToArray();
            return true;
        }

        // 模糊匹配
        regularResult = BlurredMatch(
            loweredKey,
            regularSongs);

        if (regularResult.Count > 0)
        {
            songs = regularResult.ToArray();
            return true;
        }

        // Utage 前缀匹配

        var utagePrefix = loweredKey[0];

        if (manager.UtageKanjiToSongs.TryGetValue(
                utagePrefix, out var utageSongs))
        {
            var searchKey = loweredKey[1..];
            var allowedSongs = utageSongs.ToHashSet();

            // 精确匹配
            var utageResult = MatchExact(searchKey)
                .Where(allowedSongs.Contains)
                .ToList();

            if (utageResult.Count > 0)
            {
                songs = utageResult.ToArray();
                return true;
            }

            // 模糊匹配
            utageResult = BlurredMatch(
                searchKey,
                utageSongs,
                utagePrefix);

            songs = utageResult.ToArray();
            return true;
        }

        songs = Array.Empty<MaiSongDto>();
        return true;
    }

    protected static string BuildSongListString(IEnumerable<MaiSongDto> songs, string commandHint, string hint)
    {
        var songsSet = songs.ToHashSet();

        var count = songsSet.Count;

        if (count == 0) return "未找到符合条件的乐曲";

        if (count > 50) return "匹配的歌曲数量过多，请缩小查询范围";

        var stringBuilder = new StringBuilder("找到了如下歌曲：\n");

        foreach (var maiSongDto in songsSet)
        {
            stringBuilder.Append("SID" + maiSongDto.SapphireId);
            if (maiSongDto.SongId != null)
                stringBuilder.Append(" · ID" + maiSongDto.SongId);
            stringBuilder.Append(". ");
            stringBuilder.AppendLine(NormalizeSongNameString(maiSongDto));
        }

        stringBuilder.AppendLine($"请发送指令 \"{commandHint}\" 以{hint}");

        return stringBuilder.ToString().Trim();
    }

    protected static string NormalizeSongNameString(MaiSongDto song)
    {
        var stringBuilder = new StringBuilder();
        if (song.SongType != MaiSongType.Utage)
            stringBuilder.Append($"[{song.SongType.ToString()?.ToUpper() ?? "UNKNOWN"}] ");
        stringBuilder.Append(song.SongName);
        stringBuilder.Append(" - ");
        stringBuilder.Append(song.ArtistName);

        return stringBuilder.ToString().Trim();
    }

    public static async Task SendUnauthorizedError(CqMessagePostContext context)
    {
        await SendMessage(context, ["您未授权"]);
    }

    public static async Task SendNotLoggedInError(CqMessagePostContext context)
    {
        await SendMessage(context, ["您未登录"]);
    }
}