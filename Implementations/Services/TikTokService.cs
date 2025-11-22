using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Microsoft.AspNetCore.Http;

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

    public async Task<SocialPostResult> CreatePostAsync(string accessToken, IFormFile videoFile, string title)
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

        return new SocialPostResult
        {
            Success = true,
            PostId = videoId,
            //MediaUrls = $"https://www.tiktok.com/@me/video/{videoId}", // TikTok video URL
            Permalink = $"https://www.tiktok.com/@me/video/{videoId}"
        };
    }

    public async Task<IList<TikTokVideoResponse>> GetAllPostsAsync(string accessToken, string openId, int limit = 50)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{TikTokApiBase}video/list/?open_id={openId}&max_count={limit}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"TikTok fetch videos failed: {json}");

        var rawData = JsonDocument.Parse(json).RootElement.GetProperty("data");
        var posts = new List<TikTokVideoResponse>();

        foreach (var item in rawData.EnumerateArray())
        {
            var video = new TikTokVideoResponse
            {
                VideoId = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                Description = item.TryGetProperty("desc", out var desc) ? desc.GetString() : null,
                Duration = item.TryGetProperty("duration", out var dur) ? dur.GetInt64() : 0,
                CoverImageUrl = item.TryGetProperty("video", out var videoObj) &&
                                videoObj.TryGetProperty("cover", out var cover) &&
                                cover.TryGetProperty("url", out var coverUrl)
                                ? coverUrl.GetString()
                                : null,
                PlayUrl = item.TryGetProperty("video", out var videoObj2) &&
                          videoObj2.TryGetProperty("play_addr", out var playAddr) &&
                          playAddr.TryGetProperty("url", out var url)
                          ? url.GetString()
                          : null,
                Author = item.TryGetProperty("author", out var authorObj) 
                         ? new TikTokAuthor
                         {
                             Username = authorObj.TryGetProperty("unique_id", out var uname) ? uname.GetString() : null,
                             DisplayName = authorObj.TryGetProperty("nickname", out var dname) ? dname.GetString() : null,
                             AvatarUrl = authorObj.TryGetProperty("avatar_thumb", out var avatar) &&
                                         avatar.TryGetProperty("url_list", out var urlList) &&
                                         urlList.ValueKind == JsonValueKind.Array && urlList.GetArrayLength() > 0
                                         ? urlList[0].GetString()
                                         : null
                         }
                         : null
            };

            posts.Add(video);
        }
        return posts.OrderByDescending(v => v.VideoId).ToList();
    }

    public Task<SocialPostResult> EditPostAsync(string accessToken, string videoId, string newTitle)
    {
        throw new NotSupportedException("TikTok API does not support editing a video post.");
    }

    public Task<bool> DeletePostAsync(string accessToken, string videoId)
    {
        throw new NotSupportedException("TikTok API does not support deleting a video post.");
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