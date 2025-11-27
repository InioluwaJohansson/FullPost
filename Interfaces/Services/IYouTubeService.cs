using FullPost.Models.DTOs;
using Google.Apis.YouTube.v3.Data;

namespace FullPost.Interfaces.Services;
public interface IYouTubeService
{
    Task<SocialPostResult> CreatePostAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private");
    Task<SocialPostResult> EditPostAsync(string accessToken, string videoId, string newTitle, string newDescription, string[]? newTags = null, string newPrivacy = "private");
    Task<bool> DeletePostAsync(string accessToken, string videoId);
    Task<Video?> GetPostAsync(string accessToken, string videoId);
    Task<IList<YouTubeVideoResponse>> GetAllPostsAsync(string accessToken, string channelId, int start, int limit = 10);
    Task<YouTubePlatformStats> GetStats(string accessToken);
}