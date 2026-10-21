using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands.MaiCommands;
using Sapphire.Data.DivingFishMai;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Utils;

namespace Sapphire.Commands;

public abstract class MaiCommandBase : CommandBase
{
    protected static MaiDataManager? DataManager;
    protected static DivingFishMaiDataManager? DivingFishMaiDataManager;

    protected static bool TryGetSongsByString(string key, out MaiSongDto[]? songs)
    {
        if (MaiDataManager.Instance == null)
        {
            songs = null;
            return false;
        }

        var loweredKey = key.ToLower();

        var resultList = new List<MaiSongDto>();

        var searchId = loweredKey.StartsWith("id");
        var searchSid = loweredKey.StartsWith("sid");

        var idString = loweredKey.Replace("sid", "").Replace("id", "").Trim();

        if (int.TryParse(idString, out var id))
        {
            if (searchId)
            {
                MaiDataManager.Instance.IdToSong.TryGetValue(id, out var song);
                if (song != null)
                    resultList.Add(song);
            }

            if (searchSid)
            {
                MaiDataManager.Instance.SapphireIdToSong.TryGetValue(id, out var song);
                if (song != null)
                    resultList.Add(song);
            }

            songs = resultList.ToHashSet().ToArray();
            return true;
        }

        /* 在锁定搜索范围为宴谱之前，先搜索普通歌曲，以避免类似 "xx" 只能搜到宴谱的情况 */

        var searchDx = loweredKey.StartsWith("dx");
        if (searchDx)
            loweredKey = loweredKey.Substring(2, loweredKey.Length - 2);
        var searchStd = loweredKey.StartsWith("标");
        if (searchStd)
            loweredKey = loweredKey.Substring(1, loweredKey.Length - 1);

        if (MaiDataManager.Instance.AliasToSongs.TryGetValue(loweredKey, out var songsByAliasWithoutUtageConsider))
            resultList.AddRange(songsByAliasWithoutUtageConsider);
        if (MaiDataManager.Instance.SongNameToSongs.TryGetValue(loweredKey, out var songsByTitleWithoutUtageConsider))
            resultList.AddRange(songsByTitleWithoutUtageConsider);

        if (searchDx)
        {
            var groupResult = resultList.GroupBy(x => x.SongName + x.ArtistName).Where(x => x.Count() == 1);

            foreach (var group in groupResult)
            foreach (var song in group)
                resultList.Remove(song);

            resultList.RemoveAll(x => x.SongType != MaiSongType.Dx);
        }

        if (searchStd)
        {
            var groupResult = resultList.GroupBy(x => x.SongName + x.ArtistName).Where(x => x.Count() == 1);

            foreach (var group in groupResult)
            foreach (var song in group)
                resultList.Remove(song);

            resultList.RemoveAll(x => x.SongType != MaiSongType.Std);
        }

        if (resultList.Count != 0)
        {
            songs = resultList.ToHashSet().ToArray();
            return true;
        }

        /* 标准化 key，找到指定的宴谱汉字及搜索范围 */

        var normalizedKey = SearchHelper.NormalizeCjkString(loweredKey);

        var searchUtage = normalizedKey.Length >= 1 &&
                          MaiDataManager.Instance.UtageKanjiToSongs.ContainsKey(normalizedKey[0]
                          );
        MaiSongDto[]? utageKanjiSongs = null;
        if (searchUtage)
        {
            loweredKey = loweredKey.Substring(1, loweredKey.Length - 1);
            utageKanjiSongs = MaiDataManager.Instance.UtageKanjiToSongs[normalizedKey[0]];
        }

        if (MaiDataManager.Instance.AliasToSongs.TryGetValue(loweredKey, out var songsByAlias))
            resultList.AddRange(songsByAlias);
        if (MaiDataManager.Instance.SongNameToSongs.TryGetValue(loweredKey, out var songsByTitle))
            resultList.AddRange(songsByTitle);


        /* 如果没有结果，则开始模糊匹配 */
        if (resultList.Count == 0)
        {
            var songNamePredicate = SearchCommand.CreateSongNamePredicate(loweredKey);
            var songAliasPredicate = SearchCommand.CreateSongAliasPredicate(loweredKey);

            resultList.AddRange(
                MaiDataManager.Instance.Songs.Where(x => songAliasPredicate(x) || songNamePredicate(x)));
        }

        if (!searchUtage)
        {
            resultList.RemoveAll(x => x.SongType == MaiSongType.Utage);
        }
        else
        {
            resultList.RemoveAll(x => x.SongType == MaiSongType.Dx);
            resultList.RemoveAll(x => x.SongType == MaiSongType.Std);
            resultList.RemoveAll(x => !utageKanjiSongs.Contains(x));
        }

        songs = resultList.ToHashSet().ToArray();
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