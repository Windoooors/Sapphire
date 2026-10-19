using System.Net;
using System.Text.Json;
using Sapphire.Data.DivingFishMai.Dtos;
using Serilog;

namespace Sapphire.Data.DivingFishMai;

public class DivingFishMaiDataManager
{
    public DivingFishMaiDataManager()
    {
        Instance = this;
    }

    public static DivingFishMaiDataManager? Instance { get; private set; }

    public async Task<BindingUrlResponseDto?> GetBindUserUrlAsync(long userId)
    {
        var env = ApiOperator.GetEnvironment();

        var userReference = ApiOperator.GetUserRefString(userId);

        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = env.ClientId,
            ["client_secret"] = env.ClientSecret,
            ["scope"] = "prober.records.read",
            ["subject_ref"] = userReference,
            ["binding_label"] = "test"
        };

        using var requestContent = new FormUrlEncodedContent(requestBody);

        var result = await Program.HttpClient.PostAsync($"{env.DivingFishAuthUrl}/oauth/device_authorization",
            requestContent);

        if (result.StatusCode != HttpStatusCode.OK) return null;

        var content = await result.Content.ReadAsStringAsync();

        try
        {
            var contentObject = JsonSerializer.Deserialize<BindingUrlResponseDto>(content);
            return contentObject;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred while deserializing DivingFish binding response content");

            return null;
        }
    }

    public async Task<DfUserRecordsDto?> GetUserPlayRecordsAsync(long userId)
    {
        var env = ApiOperator.GetEnvironment();

        var url = $"{env.DivingFishUrl}/api/maimaidxprober/player/records";

        var response = await ApiOperator.GetWithCredentials(url, userId);

        if (response.statusCode != HttpStatusCode.OK || response.result == null) return null;
        try
        {
            var result = JsonSerializer.Deserialize<DfUserRecordsDto>(response.result);
            return result;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred when trying to deserialize user record data from DivingFish");
        }

        return null;
    }
}