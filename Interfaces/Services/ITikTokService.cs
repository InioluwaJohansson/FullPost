using System.Text.Json;

namespace FullPost.Interfaces.Services;

public interface ITikTokService
{
    Task<string> UploadVideoAsync(string accessToken, IFormFile videoFile, string title);
    Task<bool> DeleteVideoAsync(string accessToken, string videoId);
    Task<JsonElement?> GetUserVideosAsync(string accessToken, string openId);
    Task<JsonElement?> GetUserProfileAsync(string accessToken);
}
