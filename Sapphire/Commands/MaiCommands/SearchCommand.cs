using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Utils;

namespace Sapphire.Commands.MaiCommands;

public class SearchCommand : MaiCommandBase
{
    public SearchCommand()
    {
        CommandHeads = ["search"];
        ActivationSettingsIdentifier = "mai_search.enabled";
        DirectCommandHeads = ["搜歌", "m搜歌", "search"];
        SupportedFlags =
        [
            new FlagSpec
            {
                ShortName = "-b",
                LongName = "--bpm"
            },
            new FlagSpec
            {
                ShortName = "-a",
                LongName = "--artist"
            }
        ];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var searchResult = new List<MaiSongDto>();

        if (commandContext.Flags.Count == 0 && commandContext.Argument == string.Empty)
        {
            await SendMessage(context, ["请指定查询范围"]);
            return;
        }

        var bpmRangeString = commandContext.GetFlag("--bpm");
        var bpmPredicate = CreateBpmPredicate(bpmRangeString);
        
        var songNamePredicate = CreateSongNamePredicate(commandContext.Argument);
        
        var artistRangeString = commandContext.GetFlag("--artist");
        var artistPredicate = CreateArtistPredicate(artistRangeString);
        
        var aliasPredicate = CreateSongAliasPredicate(commandContext.Argument);

        searchResult.AddRange(DataManager?.Songs.Where(x => bpmPredicate(x) && (songNamePredicate(x) || aliasPredicate(x)) && artistPredicate(x)) ?? []);
        
        await SendMessage(context, [BuildSearchResultString(searchResult)]);
    }

    public static Func<MaiSongDto, bool> CreateSongNamePredicate(string? value)
    {
        if (value?.Length == 0)
        {
            return _ => true;
        }
        
        var inputKey = SearchHelper.NormalizeCjkString(value ?? "");
        return x => MaiDataManager.Instance?.SongNameSearchKeys[x].ToLower().Contains(inputKey.ToLower()) ?? false;
    }
    
    public static Func<MaiSongDto, bool> CreateSongAliasPredicate(string? value)
    {
        if (value?.Length == 0)
        {
            return _ => true;
        }
        
        var inputKey = SearchHelper.NormalizeCjkString(value ?? "");
        return x => MaiDataManager.Instance?.AliasSearchKeys[x].Any(y => y.ToLower().Contains(inputKey.ToLower())) ??
                    false;
    }
    
    private static Func<MaiSongDto, bool> CreateArtistPredicate(string? value)
    {
        if (value?.Length == 0)
        {
            return _ => true;
        }
        
        var inputKey = SearchHelper.NormalizeCjkString(value ?? "");
        return x => MaiDataManager.Instance?.ArtistSearchKeys[x].ToLower().Contains(inputKey.ToLower()) ?? false;
    }

    private static Func<MaiSongDto, bool> CreateBpmPredicate(string? value)
    {
        if (value == null)
            return _ => true;

        if (uint.TryParse(value, out var bpm))
            return x => x.SongBpm == bpm;

        var limits = value.Split('-');

        if (limits.Length != 2)
            return _ => true;

        var lower = value.StartsWith('-')
            ? 0
            : uint.TryParse(limits[0], out var lowerValue)
                ? lowerValue
                : 0;

        var upper = value.EndsWith('-')
            ? 0
            : uint.TryParse(limits[1], out var upperValue)
                ? upperValue
                : 0;

        return x =>
            x.SongBpm >= lower &&
            (upper == 0 || x.SongBpm <= upper);
    }

    private static string BuildSearchResultString(IEnumerable<MaiSongDto> songs)
    {
        var songsSet = songs.ToHashSet();
        
        var count = songsSet.Count;

        if (count == 0)
        {
            return "未找到符合条件的乐曲";
        }
        
        if (count > 50)
        {
            return "结果过多，请缩小查询范围";
        }
        
        var stringBuilder = new StringBuilder("找到了如下歌曲：\n");

        foreach (var maiSongDto in songsSet)
        {
            stringBuilder.Append("SID" + maiSongDto.SapphireId);
            if (maiSongDto.SongId != null)
                stringBuilder.Append(" · ID" + maiSongDto.SongId);
            stringBuilder.Append(". ");
            stringBuilder.AppendLine(NormalizeSongNameString(maiSongDto));
        }
        
        return stringBuilder.ToString().Trim();
    }
}