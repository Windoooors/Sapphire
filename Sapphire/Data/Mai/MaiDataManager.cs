using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Mai.Database;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Utils;

namespace Sapphire.Data.Mai;

public class MaiDataManager
{
    public MaiDataManager()
    {
        using var context = new MaiMetadataContext();

        context.Database.Migrate();

        Songs = [];
        Aliases = [];

        UpdateSongsAndAliases(context);
        Versions = context.MaiVersions.AsNoTracking().OrderBy(x => x.VersionIndex).ToArray();
        Categories = context.MaiCategories.AsNoTracking().OrderBy(x => x.CategoryIndex).ToArray();

        BakeIndexingData();

        using var userContext = new MaiUserDataContext();
        userContext.Database.Migrate();
    }

    public static MaiDataManager? Instance { get; private set; } // Will be set to null when refreshing indexing data.

    public MaiSongDto[] Songs { get; private set; }
    public MaiAliasDto[] Aliases { get; private set; }
    public MaiVersionDto[] Versions { get; private set; }
    public MaiCategoryDto[] Categories { get; private set; }

    public Dictionary<MaiSongDto, string> SongNameSearchKeys { get; } = [];
    public Dictionary<MaiSongDto, string> ArtistSearchKeys { get; } = [];
    public Dictionary<MaiSongDto, string[]> AliasSearchKeys { get; } = [];

    public Dictionary<string, MaiSongDto[]> SongNameToSongs { get; } = [];
    public Dictionary<int, MaiSongDto> SapphireIdToSong { get; } = [];
    public Dictionary<int, MaiSongDto> IdToSong { get; } = [];
    public Dictionary<string, MaiSongDto[]> AliasToSongs { get; } = [];
    public Dictionary<MaiSongDto, MaiAliasDto[]> SongToAliases { get; } = [];
    public Dictionary<int, MaiAliasDto> AliasIdToAlias { get; } = [];
    public Dictionary<char, MaiSongDto[]> UtageKanjiToSongs { get; } = [];

    public async Task<bool> InsertUserPlayRecords(MaiPlayRecord[] records, long userId)
    {
        await using var context = new MaiUserDataContext();
        var findResult = await context.MaiUserPlayRecords.FirstOrDefaultAsync(x => x.UserId == userId);

        if (findResult == null)
        {
            findResult = new MaiUserPlayRecordDto
            {
                UserId = userId
            };
            await context.MaiUserPlayRecords.AddAsync(findResult);
        }

        findResult.Data = MaiPlayRecordBlobHelper.ToBlob(records);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<byte[]?> GetUserPlayRecordsInBytesAsync(long userId)
    {
        await using var context = new MaiUserDataContext();
        var findResult = await context.MaiUserPlayRecords.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

        return findResult?.Data;
    }

    public ReadOnlySpan<MaiPlayRecord> DecodeUserPlayRecords(byte[] data)
    {
        return MemoryMarshal.Cast<byte, MaiPlayRecord>(data);
    }

    private void BakeIndexingData()
    {
        Instance = null;

        SongNameSearchKeys.Clear();
        ArtistSearchKeys.Clear();
        AliasSearchKeys.Clear();
        SongNameToSongs.Clear();
        SapphireIdToSong.Clear();
        IdToSong.Clear();
        AliasToSongs.Clear();
        SongToAliases.Clear();
        AliasIdToAlias.Clear();
        UtageKanjiToSongs.Clear();

        UtageKanjiToSongs.TryAdd('宴', [.. Songs.Where(x => x.SongType == MaiSongType.Utage)]);

        foreach (var maiSongDto in Songs)
        {
            if (maiSongDto.SongName == null)
                continue;

            var songName = maiSongDto.SongName.ToLower();

            if (maiSongDto.SongType == MaiSongType.Utage && songName.Length >= 3)
            {
                var utageKanji = SearchHelper.NormalizeCjkString(songName)[0];

                UtageKanjiToSongs.TryAdd(utageKanji,
                [
                    .. Songs.Where(x =>
                        x.SongType == MaiSongType.Utage &&
                        SearchHelper.NormalizeCjkString(x.SongName ?? "-")[0] == utageKanji)
                ]);

                songName = songName.Substring(3, songName.Length - 3);
            }

            SongNameToSongs.TryAdd(songName, [
                .. Songs.Where(x =>
                {
                    var xSongName = x.SongName?.ToLower();

                    if (xSongName == null)
                        return false;

                    if (x.SongType == MaiSongType.Utage && xSongName.Length >= 3)
                        xSongName = xSongName.Substring(3, xSongName.Length - 3);

                    return xSongName == songName;
                })
            ]);

            SapphireIdToSong.TryAdd(maiSongDto.SapphireId, maiSongDto);

            if (maiSongDto.SongId != null)
                IdToSong.TryAdd(maiSongDto.SongId ?? 0, maiSongDto);

            SongToAliases.TryAdd(maiSongDto, [.. Aliases.Where(x => x.Song.Equals(maiSongDto))]);
        }

        foreach (var alias in Aliases)
        {
            var aliasString = alias.Alias.ToLower();

            AliasToSongs.TryAdd(aliasString,
                [.. Aliases.Where(x => x.Alias.ToLower() == aliasString).Select(y => y.Song)]);

            AliasIdToAlias.TryAdd(alias.AliasId, alias);
        }

        foreach (var maiSongDto in Songs)
        {
            var songName = maiSongDto.SongName;
            if (songName != null)
            {
                if (maiSongDto.SongType == MaiSongType.Utage && songName.Length >= 3)
                    songName = songName.Substring(3, songName.Length - 3);

                SongNameSearchKeys.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(songName));
            }

            var artistName = maiSongDto.ArtistName;
            if (artistName != null)
                ArtistSearchKeys.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(artistName));

            AliasSearchKeys.TryAdd(maiSongDto, [
                .. SongToAliases[maiSongDto].Select(x => SearchHelper.NormalizeCjkString(
                    x.Alias))
            ]);
        }

        Instance = this;
    }

    public async Task<bool> TryRemoveAlias(int aliasId, long creatorUserId, bool forced = false)
    {
        await using var context = new MaiMetadataContext();

        var alias = await context.MaiAliases.FirstOrDefaultAsync(x => x.AliasId == aliasId);

        if (alias == null)
            return false;

        if (!forced && (alias.CreatorUserId == null || alias.CreatorUserId != creatorUserId))
            return false;

        context.MaiAliases.Remove(alias);

        await context.SaveChangesAsync();

        UpdateSongsAndAliases(context);
        BakeIndexingData();

        return true;
    }

    public async Task<bool> TryAddAlias(MaiSongDto maiSongDto, string alias, long creatorUserId)
    {
        if (Aliases.AsQueryable().Any(x => x.Alias.ToLower() == alias.ToLower() && x.Song.Equals(maiSongDto)))
            return false;

        await using var context = new MaiMetadataContext();

        var song = await context.MaiSongs.FirstOrDefaultAsync(x => x.SapphireId == maiSongDto.SapphireId);

        if (song == null)
            return false;

        await context.MaiAliases.AddAsync(new MaiAliasDto
        {
            Alias = alias,
            CreatorUserId = creatorUserId,
            Song = song
        });

        await context.SaveChangesAsync();

        UpdateSongsAndAliases(context);
        BakeIndexingData();

        return true;
    }

    private void UpdateSongsAndAliases(MaiMetadataContext context)
    {
        Songs = context.MaiSongs
            .Include(x => x.Charts.OrderBy(y => y.LevelIndex))
            .OrderBy(x => x.SapphireId)
            .ToArray();

        Aliases = context.MaiAliases
            .Include(x => x.Song)
            .OrderBy(x => x.AliasId)
            .ToArray();
    }

    public static int GetRating(int chartRating, uint achievement)
    {
        return (int)(chartRating * GetRatingFactor(achievement) * (achievement > 1005000 ? 1005000 : achievement) /
                     100000000f);
    }

    public static int GetRating(int sapphireId, int levelIndex, uint achievement, bool useJpLevel = false)
    {
        if (Instance == null || !Instance.SapphireIdToSong.TryGetValue(sapphireId, out var song))
            return 0;

        if (levelIndex > song.Charts.Count - 1)
            return 0;

        var level = (useJpLevel ? song.Charts[levelIndex].JpLevel : song.Charts[levelIndex].CnLevel) ?? 0;

        return (int)(level * GetRatingFactor(achievement) * (achievement > 1005000 ? 1005000 : achievement) /
                     100000000f);
    }

    private static float GetRatingFactor(uint achievement)
    {
        return achievement switch
        {
            799999 => 12.8f,
            969999 => 17.6f,
            989999 => 20.6f,
            999999 => 21.4f,
            1004999 => 22.2f,

            >= 100000 and < 200000 => 1.6f,
            >= 200000 and < 300000 => 3.2f,
            >= 300000 and < 400000 => 4.8f,
            >= 400000 and < 500000 => 6.4f,
            >= 500000 and < 600000 => 8f,
            >= 600000 and < 700000 => 9.6f,
            >= 700000 and < 750000 => 11.2f,
            >= 750000 and < 800000 => 12f,
            >= 800000 and < 900000 => 13.6f,
            >= 900000 and < 940000 => 15.2f,
            >= 940000 and < 970000 => 16.8f,
            >= 970000 and < 980000 => 20f,
            >= 980000 and < 990000 => 20.3f,
            >= 990000 and < 995000 => 20.8f,
            >= 995000 and < 1000000 => 21.1f,
            >= 1000000 and < 1005000 => 21.6f,
            >= 1005000 => 22.4f,

            _ => 0f
        };
    }

    private static class MaiPlayRecordBlobHelper
    {
        public const int Stride = 10;

        static MaiPlayRecordBlobHelper()
        {
            var size = Unsafe.SizeOf<MaiPlayRecord>();
            if (size != Stride)
                throw new InvalidOperationException($"MaiPlayRecord must be {Stride} bytes (currently {size} bytes)");
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Big endian is not supported");
        }

        public static byte[] ToBlob(ReadOnlySpan<MaiPlayRecord> records)
        {
            var last = records.Length - 1;
            while (last >= 0 && records[last].ClearState == ClearState.NotPlayed) last--;
            if (last < 0) return [];

            return MemoryMarshal.AsBytes(records[..(last + 1)]).ToArray();
        }
    }
}