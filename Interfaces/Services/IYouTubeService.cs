using Google.Apis.YouTube.v3.Data;

namespace FullPost.Interfaces.Services;

public interface IYouTubeService
{
    Task<string> UploadVideoAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private");
    Task<bool> DeleteVideoAsync(string accessToken, string videoId);
    Task<Video?> GetVideoAsync(string accessToken, string videoId);
    Task<IList<Video>> GetUserVideosAsync(string accessToken, string channelId);
}
