namespace Sapphire.Environments.SubEnvironments;

public record ImageGenerationEnvironment
{
    public string BackgroundColorInHex { get; init; } = "#DDDDDD";
    public string FontColorInHex { get; init; } = "#000000";
    public string MiddleLayerColorInHex { get; init; } = "#FFFFFF";

    public string RemasterColorInHex { get; init; } = "#DFD6FB";
    public string MasterColorInHex { get; init; } = "#5A2989";
    public string ExpertColorInHex { get; init; } = "#C24B31";
    public string AdvancedColorInHex { get; init; } = "#EEEF47";
    public string BasicColorInHex { get; init; } = "#97FF5A";

    public string RemasterFontColorInHex { get; init; } = "#000000";
    public string MasterFontColorInHex { get; init; } = "#FFFFFF";
    public string ExpertFontColorInHex { get; init; } = "#FFFFFF";
    public string AdvancedFontColorInHex { get; init; } = "#000000";
    public string BasicFontColorInHex { get; init; } = "#000000";

    public bool PreloadCoverImages { get; init; } = true;

    public int MaxParallelGenerationTaskCount { get; init; } = 2;
}