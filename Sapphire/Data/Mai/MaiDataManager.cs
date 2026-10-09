using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Mai.Database;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Utils;

namespace Sapphire.Data.Mai;

public class MaiDataManager
{
    public static MaiDataManager? Instance { get; private set; }
    
    public MaiSongDto[] Songs { get; private set; }
    public MaiAliasDto[] Aliases { get; private set; }

    public Dictionary<MaiSongDto, string> SongNameSearchKeys { get; } = [];
    public Dictionary<MaiSongDto, string> ArtistSearchKeys { get; } = [];
    public Dictionary<MaiSongDto, string[]> AliasSearchKeys { get; } = [];
    
    public Dictionary<string, MaiSongDto[]> SongNameToSongs { get; } = [];
    public Dictionary<int, MaiSongDto> SapphireIdToSong { get; } = [];
    public Dictionary<int, MaiSongDto> IdToSong { get; } = [];
    public Dictionary<string, MaiSongDto[]> AliasToSongs { get; } = [];
    public Dictionary<MaiSongDto, MaiAliasDto[]> SongToAliases { get; } = [];
    public Dictionary<int, MaiAliasDto> AliasIdToAlias { get; } = [];

    public MaiDataManager()
    {
        using var context = new MaiMetadataContext();

        context.Database.Migrate();

        Songs = context.MaiSongs.Include(x => x.Charts).ToArray();
        Aliases = context.MaiAliases.ToArray();

        BakeIndexingData();

        using var userContext = new MaiUserDataContext();
        userContext.Database.Migrate();
    }

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
        
        foreach (var maiSongDto in Songs)
        {
            if (maiSongDto.SongName == null)
                continue;

            var songName = maiSongDto.SongName.ToLower();

            SongNameToSongs.TryAdd(songName, [.. Songs.Where(x => x.SongName?.ToLower() == songName)]);
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
            SongNameSearchKeys.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(maiSongDto.SongName ?? ""));
            ArtistSearchKeys.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(maiSongDto.ArtistName ?? ""));

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
        
        Songs = await context.MaiSongs.ToArrayAsync();
        Aliases = await context.MaiAliases.ToArrayAsync();
        
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
        
        Songs = await context.MaiSongs.ToArrayAsync();
        Aliases = await context.MaiAliases.ToArrayAsync();
        
        BakeIndexingData();

        return true;
    }
    
    private static class MaiPlayRecordBlobHelper
    {
        public const int Stride = 10;

        static MaiPlayRecordBlobHelper()
        {
            int size = Unsafe.SizeOf<MaiPlayRecord>();
            if (size != Stride)
                throw new InvalidOperationException($"MaiPlayRecord 必须是 {Stride} 字节（当前 {size}）");
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("blob 布局约定为小端");
        }

        public static byte[] ToBlob(ReadOnlySpan<MaiPlayRecord> records)
        {
            int last = records.Length - 1;
            while (last >= 0 && records[last].ClearState == ClearState.NotPlayed) last--;
            if (last < 0) return [];

            return MemoryMarshal.AsBytes(records[..(last + 1)]).ToArray();
        }
    }
}