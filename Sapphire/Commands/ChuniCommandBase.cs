using Sapphire.Data.Chuni;
using Sapphire.Data.Chuni.Database;

namespace Sapphire.Commands;

public abstract class ChuniCommandBase : CommandBase
{
    protected static ChuniDataManager? DataManager;
}