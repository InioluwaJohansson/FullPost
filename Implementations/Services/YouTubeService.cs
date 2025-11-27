using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using System.Text.Json;

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
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is invalid.");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _config["App:Name"] ?? "FullPost"
        });
    }
    public async Task<SocialPostResult> CreatePostAsync(string accessToken, IFormFile videoFile, string title, string description, string[]? tags = null, string privacy = "private")
    {
        var result = new SocialPostResult();
        try
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
                    CategoryId = "Videos"
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = privacy
                }
            };

            var mimeType = videoFile.ContentType ?? "video/*";

            var request = youtube.Videos.Insert(video, "snippet,status", stream, mimeType);
            request.ChunkSize = 256 * 1024; // 256KB chunk upload

            IUploadProgress progress;

            try
            {
                progress = await request.UploadAsync();
            }
            catch (Exception ex)
            {
                return new SocialPostResult
                {
                    Success = false,
                    RawResponse = $"YouTube upload error: {ex.Message}"
                };
            }

            if (progress.Status != UploadStatus.Completed)
            {
                return new SocialPostResult
                {
                    Success = false,
                    RawResponse = $"Upload failed: {progress.Exception?.Message}"
                };
            }

            var videoId = request.ResponseBody?.Id;

            if (videoId == null)
            {
                return new SocialPostResult
                {
                    Success = false,
                    RawResponse = "YouTube did not return a video ID."
                };
            }
            var mediaUrls = new List<string>
            {
                $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/sddefault.jpg"
            };
            return new SocialPostResult
            {
                Success = true,
                PostId = videoId,
                Permalink = $"https://www.youtube.com/watch?v={videoId}",
                MediaUrls = mediaUrls,
                RawResponse = JsonSerializer.Serialize(request.ResponseBody)
            };
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                RawResponse = ex.ToString()
            };
        }
    }
    public async Task<SocialPostResult> EditPostAsync(string accessToken,string videoId,string newTitle,string newDescription,string[]? newTags = null,string newPrivacy = "private")
    {
        try
        {
            var youtube = CreateYouTubeClient(accessToken);

            var getRequest = youtube.Videos.List("snippet,status");
            getRequest.Id = videoId;
            var response = await getRequest.ExecuteAsync();
            var video = response.Items.FirstOrDefault();
            if (video == null)
            {
                return new SocialPostResult
                {
                    Success = false,
                    RawResponse = "Video not found."
                };
            }
            video.Snippet.Title = newTitle;
            video.Snippet.Description = newDescription;
            video.Snippet.Tags = newTags;
            video.Status.PrivacyStatus = newPrivacy;
            var updateRequest = youtube.Videos.Update(video, "snippet,status");
            var updatedVideo = await updateRequest.ExecuteAsync();
            var mediaUrls = new List<string>
            {
                $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg",
                $"https://img.youtube.com/vi/{videoId}/sddefault.jpg"
            };
            return new SocialPostResult
            {
                Success = true,
                PostId = videoId,
                Permalink = $"https://www.youtube.com/watch?v={videoId}",
                MediaUrls = mediaUrls,
                RawResponse = JsonSerializer.Serialize(updatedVideo)
            };
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                RawResponse = ex.ToString()
            };
        }
    }
    public async Task<bool> DeletePostAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        await youtube.Videos.Delete(videoId).ExecuteAsync();
        return true;
    }
    public async Task<Video?> GetPostAsync(string accessToken, string videoId)
    {
        var youtube = CreateYouTubeClient(accessToken);
        var request = youtube.Videos.List("snippet,statistics,status");
        request.Id = videoId;
        var response = await request.ExecuteAsync();
        return response.Items.FirstOrDefault();
    }
    public async Task<IList<YouTubeVideoResponse>> GetAllPostsAsync(string accessToken, string channelId, int start, int limit = 50)
    {
        var youtube = CreateYouTubeClient(accessToken);
        var searchRequest = youtube.Search.List("snippet");
        searchRequest.ChannelId = channelId;
        searchRequest.MaxResults = limit;
        searchRequest.Type = "video";
        searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
        searchRequest.PageToken = start > 0 ? await GetPageTokenAsync(youtube, channelId, start) : null;
        var searchResponse = await searchRequest.ExecuteAsync();
        if (searchResponse.Items == null || searchResponse.Items.Count == 0)
            return new List<YouTubeVideoResponse>();
        var videoIds = searchResponse.Items.Select(v => v.Id.VideoId).ToList();
        var videosRequest = youtube.Videos.List("statistics,snippet");
        videosRequest.Id = string.Join(",", videoIds);
        var videosResponse = await videosRequest.ExecuteAsync();
        var posts = videosResponse.Items.Select(item => new YouTubeVideoResponse
        {
            VideoId = item.Id,
            Title = item.Snippet?.Title,
            Description = item.Snippet?.Description,
            PublishedAt = item.Snippet?.PublishedAt ?? DateTime.MinValue,
            Thumbnails = item.Snippet?.Thumbnails != null
                ? new YouTubeThumbnails
                {
                    Default = item.Snippet.Thumbnails.Default__.Url,
                    Medium = item.Snippet.Thumbnails.Medium.Url,
                    High = item.Snippet.Thumbnails.High.Url
                }
                : null,
            Views = item.Statistics?.ViewCount ?? 0,
            Likes = item.Statistics?.LikeCount ?? 0,
            Dislikes = item.Statistics?.DislikeCount ?? 0,
            Comments = item.Statistics?.CommentCount ?? 0
        }).OrderByDescending(p => p.PublishedAt).ToList();
        return posts;
    }
    private async Task<string> GetPageTokenAsync(Google.Apis.YouTube.v3.YouTubeService youtube, string channelId, int start)
    {
        if (start <= 0)
            return null;
        var searchRequest = youtube.Search.List("snippet");
        searchRequest.ChannelId = channelId;
        searchRequest.MaxResults = 50;
        searchRequest.Type = "video";
        searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
        string nextPageToken = null;
        int currentIndex = 0;
        while (true)
        {
            searchRequest.PageToken = nextPageToken;
            var response = await searchRequest.ExecuteAsync();
            if (response.Items == null || response.Items.Count == 0)
                return null;
            if (currentIndex + response.Items.Count >= start)
                return nextPageToken;
            currentIndex += response.Items.Count;
            nextPageToken = response.NextPageToken;
            if (string.IsNullOrEmpty(nextPageToken))
                return null;
        }
    }
    public async Task<YouTubePlatformStats> GetStats(string accessToken)
    {
        var youtube = CreateYouTubeClient(accessToken);
        var request = youtube.Channels.List("statistics");
        request.Mine = true;
        var response = await request.ExecuteAsync();
        var stats = response.Items[0].Statistics;
        return new YouTubePlatformStats
        {
            Followers = stats.SubscriberCount ?? 0,
            Views = stats.ViewCount ?? 0,
            Likes = 0
        };
    }
}
