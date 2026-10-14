namespace Sapphire.Environments.SubEnvironments;

public record DivingFishEnvironment
{
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";

    public string DivingFishAuthUrl { get; init; } = "https://auth.diving-fish.com";
    public string DivingFishUrl { get; init; } = "https://www.diving-fish.com";
}