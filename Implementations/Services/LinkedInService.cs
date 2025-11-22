using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;

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
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{LinkedInApiBase}me?projection=(id,localizedFirstName,localizedLastName,profilePicture(displayImage~:playableStreams))"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn profile fetch failed: {json}");

        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<JsonElement> GetUserEmailAsync(string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{LinkedInApiBase}emailAddress?q=members&projection=(elements*(handle~))"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn email fetch failed: {json}");

        return JsonDocument.Parse(json).RootElement;
    }

    public async Task<SocialPostResult> CreatePostAsync(string accessToken, string linkedInUserId, string message, string? mediaUrl = null)
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

        var json = JsonDocument.Parse(result).RootElement;
        string urn = json.GetProperty("id").GetString()!;

        return new SocialPostResult
        {
            Success = true,
            PostId = urn,
            MediaUrls = null,
            Permalink = $"https://www.linkedin.com/feed/update/{urn}"
        };
    }

    public async Task<IList<LinkedInPostResponse>> GetAllPostsAsync(string accessToken, string linkedInUserId, int start, int limit = 50)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{LinkedInApiBase}ugcPosts?q=authors&authors=List(urn:li:person:{linkedInUserId})&start={start}&count={limit}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn posts fetch failed: {json}");

        var rawData = JsonDocument.Parse(json).RootElement;
        var posts = new List<LinkedInPostResponse>();

        if (rawData.TryGetProperty("elements", out var elements))
        {
            foreach (var item in elements.EnumerateArray())
            {
                var post = new LinkedInPostResponse
                {
                    Urn = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                    Text = item.TryGetProperty("specificContent", out var content) &&
                           content.TryGetProperty("com.linkedin.ugc.ShareContent", out var shareContent) &&
                           shareContent.TryGetProperty("shareCommentary", out var commentary) &&
                           commentary.TryGetProperty("text", out var text)
                           ? text.GetString()
                           : null,
                    CreatedAt = item.TryGetProperty("created", out var created) &&
                                created.TryGetProperty("time", out var time)
                                ? DateTimeOffset.FromUnixTimeMilliseconds(time.GetInt64()).DateTime
                                : DateTime.MinValue,
                    Media = new LinkedInMedia()
                };

                if (item.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("media", out var mediaArray) &&
                    mediaArray.ValueKind == JsonValueKind.Array &&
                    mediaArray.GetArrayLength() > 0)
                {
                    var mediaItem = mediaArray[0];
                    post.Media.MediaType = mediaItem.TryGetProperty("mediaType", out var type) ? type.GetString() : null;
                    post.Media.Url = mediaItem.TryGetProperty("url", out var url) ? url.GetString() : null;
                }

                posts.Add(post);
            }
        }
        return posts.OrderByDescending(p => p.CreatedAt).ToList();
    }
    public async Task<SocialPostResult> EditPostAsync(string accessToken, string postUrn, string linkedInUserId, string newMessage, string? mediaUrl = null)
    {
        await DeletePostAsync(accessToken, postUrn);

        var newPost = await CreatePostAsync(accessToken, linkedInUserId, newMessage, mediaUrl);

        return new SocialPostResult
        {
            Success = true,
            PostId = newPost.PostId,
            Permalink = newPost.Permalink,
            MediaUrls = newPost.MediaUrls,
            RawResponse = newPost.RawResponse
        };
    }
    public async Task<bool> DeletePostAsync(string accessToken, string postUrn)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{LinkedInApiBase}ugcPosts/{postUrn}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"LinkedIn delete failed: {result}");

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