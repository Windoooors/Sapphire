using System.Net.Http.Json;
using System.Text.Json;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Wck.Dtos;
using Sapphire.Environments;

namespace Sapphire.Commands.MaiCommands;

public class WckSessionLoginCommand : MaiCommandBase // Wck stands for WahlapConnectiveKits
{
    public WckSessionLoginCommand()
    {
        CommandHeads = ["login"];
        DirectCommandHeads = ["login"];

        ActivationSettingsIdentifier = "mai_wck_login.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        try
        {
            var env = SpEnvironmentHandler.Environment.WahlapConnectiveKits;

            var request = new HttpRequestMessage(HttpMethod.Post, $"{env.WckUrl}/v1/sessions");
            request.Content = JsonContent.Create(new SessionLoginRequestDto
            {
                WechatQrCode = commandContext.Argument ?? "",
                QqId = context.UserId
            });

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(10));

            var resp = await Program.HttpClient.SendAsync(request, cts.Token);

            var responseString = await resp.Content.ReadAsStringAsync();

            var responseObject = JsonSerializer.Deserialize<SessionLoginResponseDto>(responseString);

            if (responseObject == null)
            {
                await SendUnknownError(context);
                return;
            }

            if (responseObject.Code != 200)
            {
                switch (responseObject.Code)
                {
                    case 402:
                        await SendMessage(context, [
                            new CqTextMsg("登录失败！可能是因为此账号没有登出")
                        ]);
                        break;
                    case 403:
                        await SendMessage(context, [
                            new CqTextMsg("登录失败！此二维码已过期")
                        ]);
                        break;
                }

                return;
            }

            await SendMessage(context, [
                new CqTextMsg("登录成功！\n使用查询功能完毕后请记得登出，否则无法登入游戏\n如要登出，请在此发送指令 \"logout\"")
            ]);
        }
        catch (Exception)
        {
            await SendUnknownError(context);
        }
    }
}