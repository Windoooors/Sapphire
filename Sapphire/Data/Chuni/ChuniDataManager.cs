using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Chuni.Database;
using Sapphire.Data.Chuni.Dtos;

namespace Sapphire.Data.Chuni;

public class ChuniDataManager
{
    public ChuniDataManager()
    {
        using var context = new ChuniMetadataContext();

        context.Database.Migrate();

        Songs = context.ChuniSongs.ToArray();

        Instance = this;
    }

    public static ChuniDataManager? Instance { get; private set; }

    public ChuniSongDto[] Songs { get; init; }
}