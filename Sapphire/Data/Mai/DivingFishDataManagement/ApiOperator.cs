using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sapphire.Data.Mai.Dtos.DivingFish;
using Sapphire.Environments;
using Sapphire.Environments.SubEnvironments;
using Serilog;

namespace Sapphire.Data.Mai;

public static class ApiOperator
{
    private static Dictionary<long, (string, DateTime)> _userCredentials = [];

    public static async Task<(string? result, HttpStatusCode? statusCode)> PostWithCredentials(string url, Dictionary<string, string> requestBody, long userId)
    {
        if (!_userCredentials.TryGetValue(userId, out var userCredential) ||
            DateTime.UtcNow >= userCredential.Item2)
        {
            var bound = await FetchUserCredentialAsync(userId);
            if (!bound)
            {
                return (null, null);
            }
            
            userCredential = _userCredentials[userId];
        }
        
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userCredential.Item1);
        request.Content = new FormUrlEncodedContent(requestBody);
        
        var response = await Program.HttpClient.SendAsync(request);
        var stringResult = await response.Content.ReadAsStringAsync();

        return (stringResult, response.StatusCode);
    }
    
    public static async Task<(string? result, HttpStatusCode? statusCode)> GetWithCredentials(string url, long userId)
    {
        if (!_userCredentials.TryGetValue(userId, out var userCredential) ||
            DateTime.UtcNow >= userCredential.Item2)
        {
            var bound = await FetchUserCredentialAsync(userId);
            if (!bound)
            {
                return (null, null);
            }
            
            userCredential = _userCredentials[userId];
        }
        
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userCredential.Item1);
        
        var response = await Program.HttpClient.SendAsync(request);
        var stringResult = await response.Content.ReadAsStringAsync();

        return (stringResult, response.StatusCode);
    }

    private static async Task<bool> FetchUserCredentialAsync(long userId)
    {
        var env = GetEnvironment();

        var refString = GetUserRefString(userId);

        var requestContent = new Dictionary<string, string>()
        {
            ["grant_type"] = "urn:diving-fish:params:oauth:grant-type:on-behalf-of",
            ["client_id"] = env.ClientId,
            ["client_secret"] = env.ClientSecret,
            ["scope"] = "prober.records.read",
            ["subject"] = "ref:" + refString
        };

        var response = await Program.HttpClient.PostAsync($"{env.DivingFishAuthUrl}/oauth/token",
            new FormUrlEncodedContent(requestContent));

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }

        var responseContentString = await response.Content.ReadAsStringAsync();

        try
        {
            var contentObject = JsonSerializer.Deserialize<AccessTokenDto>(responseContentString);
            if (contentObject == null)
                return false;

            var item = (contentObject.AccessToken, DateTime.UtcNow.AddSeconds(contentObject.ExpiresIn - 30));
            
            if (!_userCredentials.TryAdd(userId, item))
                _userCredentials[userId] = item;
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occured while deserializing DivingFish user access token");
            return false;
        }
    }

    public static DivingFishEnvironment GetEnvironment()
    {
        if (SpEnvironmentHandler.Instance == null)
        {
            throw new Exception("SpEnvironmentHandler.Instance is null");
        }

        return SpEnvironmentHandler.Instance.Environment.DivingFish;
    }
    
    public static string GetUserRefString(long userId)
    {
        if (SpEnvironmentHandler.Instance == null)
        {
            throw new Exception("SpEnvironmentHandler.Instance is null");
        }

        byte[] bytes =
            Encoding.UTF8.GetBytes(SpEnvironmentHandler.Instance.Environment.DivingFish.ClientId + userId);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}