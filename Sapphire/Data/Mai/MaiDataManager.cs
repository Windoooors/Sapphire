using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Mai.Database;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Utils;

namespace Sapphire.Data.Mai;

public class MaiDataManager
{
    public static MaiDataManager? Instance { get; private set; }
    
    public MaiSongDto[] Songs { get; init; }

    public Dictionary<MaiSongDto, string> SongNameSearchKeys { get; } = [];
    public Dictionary<MaiSongDto, string> ArtistSearchValues { get; } = [];
    private Dictionary<string, MaiSongDto[]> SongNameToSongs { get; } = [];
    private Dictionary<int, MaiSongDto> SapphireIdToSong { get; } = [];
    private Dictionary<int, MaiSongDto> IdToSong { get; } = [];
    
    public MaiDataManager()
    {
        using var context = new MaiMetadataContext();

        context.Database.Migrate();

        Songs = context.MaiSongs.ToArray();
        
        foreach (var maiSongDto in Songs)
        {
            SongNameSearchKeys.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(maiSongDto.SongName ?? ""));
            ArtistSearchValues.TryAdd(maiSongDto, SearchHelper.NormalizeCjkString(maiSongDto.ArtistName ?? ""));
        }
        
        Instance = this;
    }
}