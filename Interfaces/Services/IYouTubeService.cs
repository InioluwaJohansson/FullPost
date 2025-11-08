using Google.Apis.YouTube.v3.Data;

namespace FullPost.Interfaces.Services;
public interface IYouTubeService
{
    Task<string> CreatePostAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private");
    Task<bool> EditPostAsync(string accessToken, string videoId, string newTitle, string newDescription, string[]? newTags = null, string newPrivacy = "private");
    Task<bool> DeletePostAsync(string accessToken, string videoId);
    Task<Video?> GetPostAsync(string accessToken, string videoId);
    Task<IList<Video>> GetAllPostsAsync(string accessToken, string channelId, int limit = 10);
}