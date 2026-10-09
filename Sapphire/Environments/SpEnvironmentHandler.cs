using Sapphire.Environments.SubEnvironments;
using Tomlyn;

namespace Sapphire.Environments;

public class SpEnvironmentHandler
{
    private const string ConfigurationFileName = "config.toml";

    public SpEnvironmentHandler()
    {
        Instance = this;

        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);

        if (!File.Exists(path) ||
            !TomlSerializer.TryDeserialize<SpEnvironment>(File.ReadAllText(path), out var environment))
        {
            Environment = new SpEnvironment
            {
                NapCat = new NapCatEnvironment(),
                Database = new DatabaseEnvironment(),
                DivingFish = new DivingFishEnvironment()
            };
            
            File.WriteAllText(path, TomlSerializer.Serialize(Environment));
            
            return;
        }

        Environment = environment;
    }

    public SpEnvironment Environment { get; init; }

    public static SpEnvironmentHandler? Instance { get; private set; }
}