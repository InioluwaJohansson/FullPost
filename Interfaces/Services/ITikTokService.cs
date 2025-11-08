using System.Text.Json;

namespace FullPost.Interfaces.Services;

public interface ITikTokService
{
    Task<string> CreatePostAsync(string accessToken, IFormFile videoFile, string title);
    Task<JsonElement?> GetAllPostsAsync(string accessToken, string openId);
    Task<bool> EditPostAsync(string accessToken, string videoId, string newTitle);
    Task<bool> DeletePostAsync(string accessToken, string videoId);
    Task<JsonElement?> GetUserProfileAsync(string accessToken);
}
