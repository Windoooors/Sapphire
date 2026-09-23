using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Chuni.Database;
using Sapphire.Data.Chuni.Dtos;

namespace Sapphire.Data.Chuni;

public class ChuniDataManager
{
    public static ChuniDataManager? Instance { get; private set; }
    
    public ChuniSongDto[] Songs { get; init; }
    
    public ChuniDataManager()
    {
        using var context = new ChuniMetadataContext();

        context.Database.Migrate();

        Songs = context.ChuniSongs.ToArray();
        
        Instance = this;
    }
}