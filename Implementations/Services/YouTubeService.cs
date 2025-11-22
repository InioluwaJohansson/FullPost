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

    public async Task<SocialPostResult> CreatePostAsync(
    string accessToken,
    IFormFile videoFile,
    string title,
    string description,
    string[]? tags = null,
    string privacy = "private")
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
                    CategoryId = "22"
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
        var result = new SocialPostResult();

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

    public async Task<IList<YouTubeVideoResponse>> GetAllPostsAsync(string accessToken, string channelId, int limit = 30)
    {
        var youtube = CreateYouTubeClient(accessToken);

        var request = youtube.Search.List("snippet");
        request.ChannelId = channelId;
        request.MaxResults = limit;
        request.Type = "video";
        request.Order = SearchResource.ListRequest.OrderEnum.Date;

        var response = await request.ExecuteAsync();
        var posts = new List<YouTubeVideoResponse>();
        foreach (var item in response.Items)
        {
            posts.Add(new YouTubeVideoResponse
            {
                VideoId = item.Id.ToString(),
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
                    : null
            });
        }
        return posts.OrderByDescending(p => p.PublishedAt).ToList();
    }
}
