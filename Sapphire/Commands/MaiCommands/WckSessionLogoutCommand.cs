using System.Net;
using System.Text.Json;
using Sapphire.Data.Wck;
using Sapphire.Data.Wck.Dtos;
using Sapphire.Environments;

namespace Sapphire.Commands.MaiCommands;

public class WckSessionLogoutCommand : MaiCommandBase
{
    public WckSessionLogoutCommand()
    {
        CommandHeads = ["logout"];
        DirectCommandHeads = ["logout"];

        ActivationSettingsIdentifier = "mai_wck_logout.enabled";
    }

    protected override async Task Process(MessageContext context)
    {
        try
        {
            var sessionId = await WckDataManager.GetSessionId(context);
            if (sessionId == null) return;

            var env = SpEnvironmentHandler.Environment.WahlapConnectiveKits;


            var request = new HttpRequestMessage(HttpMethod.Delete, $"{env.WckUrl}/v1/sessions?session_id={sessionId}");

            using var resp = await Program.HttpClient.SendAsync(request);
            var respBody = await resp.Content.ReadAsStringAsync();

            var responseObject = JsonSerializer.Deserialize<SessionLogoutResponseDto>(respBody);

            if (responseObject == null)
            {
                await SendUnknownError(context);
                return;
            }

            if (responseObject.Code != 200)
            {
                await SendMessage(context, "登出失败！");
                return;
            }

            await SendMessage(context, "登出成功！");
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest })
            {
                await SendMessage(context, "登出失败！");
                return;
            }

            await SendUnknownError(context);
        }
    }
}