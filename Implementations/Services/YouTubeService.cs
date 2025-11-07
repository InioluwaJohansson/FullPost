using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using FullPost.Interfaces.Services;
namespace FullPost.Implementations.Services;
public class YouTubeService : IYouTubeService
{
    private readonly IConfiguration _config;
    public YouTubeService(IConfiguration config)
    {
        _config = config;
    }

    private Google.Apis.YouTube.v3.YouTubeService CreateYouTubeClient(string accessToken)
    {
        var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken(accessToken);

        return new Google.Apis.YouTube.v3.YouTubeService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _config["App:Name"]
        });
    }


    public async Task<string> UploadVideoAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private")
    {
        var youtube = CreateYouTubeClient(accessToken);

        using var stream = videoFile.OpenReadStream();
        var video = new Video
        {
            Snippet = new VideoSnippet
            {
                Title = title,
                Description = description,
                Tags = tags,
                CategoryId = "22" // 22 = People & Blogs
            },
            Status = new VideoStatus { PrivacyStatus = privacy }
        };

        var videosInsertRequest = youtube.Videos.Insert(video, "snippet,status", stream, "video/*");
        var uploadProgress = await videosInsertRequest.UploadAsync();

        if (uploadProgress.Status == UploadStatus.Completed)
            return videosInsertRequest.ResponseBody.Id;
        else
            throw new Exception($"YouTube upload failed: {uploadProgress.Exception?.Message}");
    }

    public async Task<bool> DeleteVideoAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        await youtube.Videos.Delete(videoId).ExecuteAsync();
        return true;
    }

    public async Task<Video?> GetVideoAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        var request = youtube.Videos.List("snippet,statistics,status");
        request.Id = videoId;

        var response = await request.ExecuteAsync();
        return response.Items.FirstOrDefault();
    }

    public async Task<IList<Video>> GetUserVideosAsync(string accessToken, string channelId)
    {
        var youtube = CreateYouTubeClient(accessToken);

        var request = youtube.Search.List("snippet");
        request.ChannelId = channelId;
        request.MaxResults = 10;
        request.Type = "video";

        var response = await request.ExecuteAsync();

        var videos = response.Items
            .Select(i => new Video
            {
                Id = i.Id.VideoId, // ✅ Use the string video ID
                Snippet = new Google.Apis.YouTube.v3.Data.VideoSnippet
                {
                    Title = i.Snippet.Title,
                    Description = i.Snippet.Description,
                    Thumbnails = i.Snippet.Thumbnails,
                    PublishedAt = i.Snippet.PublishedAt
                }
            })
            .ToList();

        return videos;
    }
}