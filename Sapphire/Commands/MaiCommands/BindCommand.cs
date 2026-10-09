using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.DivingFishDataManagement;

namespace Sapphire.Commands.MaiCommands;

public class BindCommand : MaiCommandBase
{
    public BindCommand()
    {
        CommandHeads = ["bind"];
        SupportedFlags = new List<FlagSpec>()
        {
            new FlagSpec()
            {
                LongName = "--divingfish",
                ShortName = "-df",
            }
        };
        
        ActivationSettingsIdentifier = "mai_bind.enabled";
    }
    
    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--divingfish"))
        {
            if (DivingFishMaiDataManager.Instance == null)
            {
                await SendUnknownError(context);
                return;
            }
            
            var urlResult = await DivingFishMaiDataManager.Instance.GetBindUserUrlAsync(context.UserId);

            if (urlResult == null)
            {
                await SendUnknownError(context);
                return;
            }

            await SendMessage(context,
                [$"请在 {TimeSpan.FromSeconds(urlResult.ExpirationPeriod).TotalMinutes :0} 分钟内访问 {urlResult.CompleteUrl} 来完成绑定"]);
            return;
        }
        
        await SendArgumentError(context);
    }
}