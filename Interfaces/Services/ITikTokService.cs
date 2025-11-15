using System.Text.Json;
using FullPost.Models.DTOs;

namespace FullPost.Interfaces.Services;

public interface ITikTokService
{
    Task<SocialPostResult> CreatePostAsync(string accessToken, IFormFile videoFile, string title);
    Task<JsonElement?> GetAllPostsAsync(string accessToken, string openId);
    Task<SocialPostResult> EditPostAsync(string accessToken, string videoId, string newTitle);
    Task<bool> DeletePostAsync(string accessToken, string videoId);
    Task<JsonElement?> GetUserProfileAsync(string accessToken);
}
