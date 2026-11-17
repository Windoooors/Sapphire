namespace Sapphire.Commands;

public class HelpCommand : CommandBase
{
    public HelpCommand()
    {
        CommandHeads = ["help"];
        DirectCommandHeads = ["help", "/help"];

        ActivationSettingsIdentifier = "help.enabled";
    }

    protected override async Task Process(MessageContext context)
    {
        await SendHelp(context);
    }
}