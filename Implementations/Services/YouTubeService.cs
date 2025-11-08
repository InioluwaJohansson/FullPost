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
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _config["App:Name"]
        });
    }

    // ✅ CREATE — Upload a new YouTube video
    public async Task<string> CreatePostAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private")
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
                CategoryId = "22" // "People & Blogs" by default
            },
            Status = new VideoStatus { PrivacyStatus = privacy }
        };

        var videosInsertRequest = youtube.Videos.Insert(video, "snippet,status", stream, "video/*");
        var uploadProgress = await videosInsertRequest.UploadAsync();

        if (uploadProgress.Status == UploadStatus.Completed)
            return videosInsertRequest.ResponseBody.Id ?? throw new Exception("Upload failed: no video ID returned.");

        throw new Exception($"YouTube upload failed: {uploadProgress.Exception?.Message}");
    }

    // ✅ EDIT — Update video title, description, tags, privacy, etc.
    public async Task<bool> EditPostAsync(string accessToken, string videoId, string newTitle, string newDescription, string[]? newTags = null, string newPrivacy = "private")
    {
        var youtube = CreateYouTubeClient(accessToken);

        var getRequest = youtube.Videos.List("snippet,status");
        getRequest.Id = videoId;
        var response = await getRequest.ExecuteAsync();
        var video = response.Items.FirstOrDefault();

        if (video == null)
            throw new Exception("Video not found.");

        video.Snippet.Title = newTitle;
        video.Snippet.Description = newDescription;
        video.Snippet.Tags = newTags;
        video.Status.PrivacyStatus = newPrivacy;

        var updateRequest = youtube.Videos.Update(video, "snippet,status");
        await updateRequest.ExecuteAsync();

        return true;
    }

    // ✅ DELETE — Remove a YouTube video
    public async Task<bool> DeletePostAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        await youtube.Videos.Delete(videoId).ExecuteAsync();
        return true;
    }

    // ✅ GET SINGLE VIDEO — Fetch one post
    public async Task<Video?> GetPostAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        var request = youtube.Videos.List("snippet,statistics,status");
        request.Id = videoId;

        var response = await request.ExecuteAsync();
        return response.Items.FirstOrDefault();
    }

    // ✅ GET ALL USER VIDEOS — Fetch all uploaded posts
    public async Task<IList<Video>> GetAllPostsAsync(string accessToken, string channelId, int limit = 10)
    {
        var youtube = CreateYouTubeClient(accessToken);

        var request = youtube.Search.List("snippet");
        request.ChannelId = channelId;
        request.MaxResults = limit;
        request.Type = "video";

        var response = await request.ExecuteAsync();

        return response.Items.Select(i => new Video
        {
            Id = i.Id.VideoId,
            Snippet = new VideoSnippet
            {
                Title = i.Snippet.Title,
                Description = i.Snippet.Description,
                Thumbnails = i.Snippet.Thumbnails,
                PublishedAt = i.Snippet.PublishedAt
            }
        }).ToList();
    }
}