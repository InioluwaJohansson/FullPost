using System.Text.Json;
using FullPost.Models.DTOs;

namespace FullPost.Interfaces.Services;

public interface ITikTokService
{
    Task<SocialPostResult> CreatePostAsync(string accessToken, IFormFile videoFile, string title);
    Task<IList<TikTokVideoResponse>> GetAllPostsAsync(string accessToken, string openId, int start, int limit = 50);
    Task<SocialPostResult> EditPostAsync(string accessToken, string videoId, string newTitle);
    Task<bool> DeletePostAsync(string accessToken, string videoId);
    Task<JsonElement?> GetUserProfileAsync(string accessToken);
    Task<PlatformStats> GetStats(string accessToken, string openId, int limit = int.MaxValue);
}
