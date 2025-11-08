using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Interfaces.Services;

namespace FullPost.Implementations.Services;
public class TikTokService : ITikTokService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public TikTokService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    private string ApiKey => _config["TikTok:ClientKey"]!;
    private string ApiSecret => _config["TikTok:ClientSecret"]!;
    private const string TikTokApiBase = "https://open.tiktokapis.com/v2/";

    public async Task<string> CreatePostAsync(string accessToken, IFormFile videoFile, string title)
    {
        var initRequest = new HttpRequestMessage(HttpMethod.Post, $"{TikTokApiBase}video/init/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var initResponse = await _httpClient.SendAsync(initRequest);
        var initJson = await initResponse.Content.ReadAsStringAsync();

        if (!initResponse.IsSuccessStatusCode)
            throw new Exception($"TikTok init upload failed: {initJson}");

        var initObj = JsonDocument.Parse(initJson).RootElement;
        var uploadUrl = initObj.GetProperty("data").GetProperty("upload_url").GetString();
        var videoId = initObj.GetProperty("data").GetProperty("video_id").GetString();

        using (var stream = videoFile.OpenReadStream())
        {
            var uploadContent = new StreamContent(stream);
            uploadContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            var uploadResponse = await _httpClient.PutAsync(uploadUrl, uploadContent);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var error = await uploadResponse.Content.ReadAsStringAsync();
                throw new Exception($"TikTok video upload failed: {error}");
            }
        }

        var publishData = new
        {
            video_id = videoId,
            text = title
        };

        var publishRequest = new HttpRequestMessage(HttpMethod.Post, $"{TikTokApiBase}video/publish/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent(JsonSerializer.Serialize(publishData), Encoding.UTF8, "application/json")
        };

        var publishResponse = await _httpClient.SendAsync(publishRequest);
        var publishJson = await publishResponse.Content.ReadAsStringAsync();

        if (!publishResponse.IsSuccessStatusCode)
            throw new Exception($"TikTok publish failed: {publishJson}");

        return videoId!;
    }

    public async Task<JsonElement?> GetAllPostsAsync(string accessToken, string openId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{TikTokApiBase}video/list/?open_id={openId}&max_count=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"TikTok fetch videos failed: {json}");

        return JsonDocument.Parse(json).RootElement.GetProperty("data");
    }

    public async Task<bool> EditPostAsync(string accessToken, string videoId, string newTitle)
    {

        throw new NotSupportedException("TikTok API does not currently support editing or updating a video post.");
    }

    public async Task<bool> DeletePostAsync(string accessToken, string videoId)
    {
        throw new NotSupportedException("TikTok API does not currently support deleting a video post.");
    }

    public async Task<JsonElement?> GetUserProfileAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{TikTokApiBase}user/info/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"TikTok user info failed: {json}");

        return JsonDocument.Parse(json).RootElement.GetProperty("data");
    }
}