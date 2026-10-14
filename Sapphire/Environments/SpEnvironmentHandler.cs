using Sapphire.Environments.SubEnvironments;
using Tomlyn;

namespace Sapphire.Environments;

public static class SpEnvironmentHandler
{
    private const string ConfigurationFileName = "config.toml";

    static SpEnvironmentHandler()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);

        if (!File.Exists(path) ||
            !TomlSerializer.TryDeserialize<SpEnvironment>(File.ReadAllText(path), out var environment))
        {
            Environment = new SpEnvironment
            {
                NapCat = new NapCatEnvironment(),
                Database = new DatabaseEnvironment(),
                DivingFish = new DivingFishEnvironment(),
                Mai = new MaiEnvironment(),
                WahlapConnectiveKits = new WahlapConnectiveKitsEnvironment(),
                ImageGeneration = new ImageGenerationEnvironment(),
                Sapphire = new SapphireEnvironment()
            };

            File.WriteAllText(path, TomlSerializer.Serialize(Environment));

            return;
        }

        Environment = environment;
    }

    public static SpEnvironment Environment { get; }
}