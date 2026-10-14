using System.Net;
using System.Text.Json;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands;
using Sapphire.Data.Wck.Dtos;
using Sapphire.Environments;

namespace Sapphire.Data.Wck;

public class WckDataManager : MaiCommandBase // Wck stands for WahlapConnectiveKits
{
    public static async Task<string?> GetSessionId(CqMessagePostContext context, bool sendHelp = true)
    {
        try
        {
            return await GetSessionIdCore(context.UserId);
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest })
            {
                if (sendHelp) await SendNotLoggedInError(context);
                return null;
            }

            if (sendHelp) await SendUnknownError(context);
            return null;
        }
    }

    public static async Task<WckMusicDataResponseItemDto[]?> GetUserScores(string sessionId)
    {
        var env = SpEnvironmentHandler.Environment.WahlapConnectiveKits;

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{env.WckUrl}/v1/user_music_data?session_id={sessionId}&range_from=0&range_to=200000");

        var resp = await Program.HttpClient.SendAsync(request);
        if (resp.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Unexpected status code: {resp.StatusCode}", null,
                resp.StatusCode);
        var responseString = await resp.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<WckMusicDataResponseDto>(responseString);
        if (responseObject?.Code != 200)
            return null;

        return responseObject.MusicData;
    }

    private static async Task<string?> GetSessionIdCore(long userId)
    {
        var env = SpEnvironmentHandler.Environment.WahlapConnectiveKits;

        var request = new HttpRequestMessage(HttpMethod.Get, $"{env.WckUrl}/v1/sessions?qq_id=" + userId);

        var resp = await Program.HttpClient.SendAsync(request);

        if (resp.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Unexpected status code: {resp.StatusCode}", null,
                resp.StatusCode);

        var responseString = await resp.Content.ReadAsStringAsync();

        var responseObject = JsonSerializer.Deserialize<SessionDto>(responseString);

        if (responseObject?.Code != 200) return null;

        return responseObject.SessionId;
    }
}