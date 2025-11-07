using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Interfaces.Services;
namespace FullPost.Implementations.Services;
public class LinkedInService : ILinkedInService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public LinkedInService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    private string ClientId => _config["LinkedIn:ClientId"]!;
    private string ClientSecret => _config["LinkedIn:ClientSecret"]!;
    private string RedirectUri => _config["LinkedIn:RedirectUri"]!;

    private const string LinkedInApiBase = "https://api.linkedin.com/v2/";

    public async Task<JsonElement> GetUserProfileAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{LinkedInApiBase}me?projection=(id,localizedFirstName,localizedLastName,profilePicture(displayImage~:playableStreams))");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn profile fetch failed: {json}");

        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<JsonElement> GetUserEmailAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{LinkedInApiBase}emailAddress?q=members&projection=(elements*(handle~))");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn email fetch failed: {json}");

        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<bool> PostContentAsync(string accessToken, string linkedInUserId, string message, string? mediaUrl = null)
    {
        var postData = new
        {
            author = $"urn:li:person:{linkedInUserId}",
            lifecycleState = "PUBLISHED",
            specificContent = new
            {
                @namespace = "com.linkedin.ugc.ShareContent",
                shareCommentary = new { text = message },
                shareMediaCategory = string.IsNullOrEmpty(mediaUrl) ? "NONE" : "IMAGE",
                media = string.IsNullOrEmpty(mediaUrl)
                    ? null
                    : new[]
                    {
                        new
                        {
                            status = "READY",
                            originalUrl = mediaUrl
                        }
                    }
            },
            visibility = new
            {
                @namespace = "com.linkedin.ugc.MemberNetworkVisibility",
                value = "PUBLIC"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{LinkedInApiBase}ugcPosts")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent(JsonSerializer.Serialize(postData), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn post failed: {result}");

        return true;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", RedirectUri},
                {"client_id", ClientId},
                {"client_secret", ClientSecret}
            })
        };

        var response = await _httpClient.SendAsync(tokenRequest);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn token exchange failed: {json}");

        var obj = JsonDocument.Parse(json).RootElement;
        return obj.GetProperty("access_token").GetString()!;
    }
}
