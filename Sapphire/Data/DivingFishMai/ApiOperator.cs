using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sapphire.Data.DivingFishMai.Dtos;
using Sapphire.Environments;
using Sapphire.Environments.SubEnvironments;
using Serilog;

namespace Sapphire.Data.DivingFishMai;

public static class ApiOperator
{
    private static readonly Dictionary<long, (string, DateTime)> UserCredentials = [];

    public static async Task<(string? result, HttpStatusCode? statusCode)> PostWithCredentials(string url,
        Dictionary<string, string> requestBody, long userId, bool write = false)
    {
        if (!UserCredentials.TryGetValue(userId, out var userCredential) ||
            DateTime.UtcNow >= userCredential.Item2)
        {
            var bound = await FetchUserCredentialAsync(userId);
            if (!bound) return (null, null);

            userCredential = UserCredentials[userId];
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userCredential.Item1);
        request.Content = new FormUrlEncodedContent(requestBody);

        var response = await Program.HttpClient.SendAsync(request);
        var stringResult = await response.Content.ReadAsStringAsync();

        return (stringResult, response.StatusCode);
    }

    public static async Task<(string? result, HttpStatusCode? statusCode)> PostWithCredentials(string url,
        HttpContent requestContent, long userId)
    {
        if (!UserCredentials.TryGetValue(userId, out var userCredential) ||
            DateTime.UtcNow >= userCredential.Item2)
        {
            var bound = await FetchUserCredentialAsync(userId);
            if (!bound) return (null, null);

            userCredential = UserCredentials[userId];
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userCredential.Item1);
        request.Content = requestContent;

        var response = await Program.HttpClient.SendAsync(request);
        var stringResult = await response.Content.ReadAsStringAsync();

        return (stringResult, response.StatusCode);
    }
    
    public static async Task<(string? result, HttpStatusCode? statusCode)> GetWithCredentials(string url, long userId)
    {
        if (!UserCredentials.TryGetValue(userId, out var userCredential) ||
            DateTime.UtcNow >= userCredential.Item2)
        {
            var bound = await FetchUserCredentialAsync(userId);
            if (!bound) return (null, null);

            userCredential = UserCredentials[userId];
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userCredential.Item1);

        var response = await Program.HttpClient.SendAsync(request);
        var stringResult = await response.Content.ReadAsStringAsync();

        return (stringResult, response.StatusCode);
    }

    private static async Task<bool> FetchUserCredentialAsync(long userId)
    {
        var env = GetEnvironment();

        var refString = GetUserRefString(userId);

        var requestContent = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:diving-fish:params:oauth:grant-type:on-behalf-of",
            ["client_id"] = env.ClientId,
            ["client_secret"] = env.ClientSecret,
            ["scope"] = "prober.records.write prober.records.read",
            ["subject"] = "ref:" + refString
        };

        var response = await Program.HttpClient.PostAsync($"{env.DivingFishAuthUrl}/oauth/token",
            new FormUrlEncodedContent(requestContent));

        if (response.StatusCode != HttpStatusCode.OK) return false;

        var responseContentString = await response.Content.ReadAsStringAsync();

        try
        {
            var contentObject = JsonSerializer.Deserialize<AccessTokenDto>(responseContentString);
            if (contentObject == null)
                return false;

            var item = (contentObject.AccessToken, DateTime.UtcNow.AddSeconds(contentObject.ExpiresIn - 30));

            if (!UserCredentials.TryAdd(userId, item))
                UserCredentials[userId] = item;
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred while deserializing DivingFish user access token");
            return false;
        }
    }

    public static DivingFishEnvironment GetEnvironment()
    {
        return SpEnvironmentHandler.Environment.DivingFish;
    }

    public static string GetUserRefString(long userId)
    {
        var bytes =
            Encoding.UTF8.GetBytes(SpEnvironmentHandler.Environment.DivingFish.ClientId + userId);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}