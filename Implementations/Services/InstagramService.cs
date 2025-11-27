using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Microsoft.Extensions.Options;

namespace FullPost.Implementations.Services;

public class InstagramService : IInstagramService
{
    private readonly Cloudinary _cloudinary;
    private readonly HttpClient _httpClient;

    public InstagramService(IConfiguration config)
    {
        var cloudSettings = config.GetSection("CloudinarySettings").Get<CloudinarySettings>();

        var account = new Account(cloudSettings.CloudName, cloudSettings.ApiKey, cloudSettings.ApiSecret);

        _cloudinary = new Cloudinary(account);
        _httpClient = new HttpClient();
    }
    private async Task<string> UploadToCloudinaryAsync(IFormFile file)
    {
        RawUploadResult uploadResult;

        using (var stream = file.OpenReadStream())
        {
            bool isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

            if (isVideo)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "instagram_uploads",
                    UseFilename = true,
                    UniqueFilename = true
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "instagram_uploads",
                    UseFilename = true,
                    UniqueFilename = true
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
        }

        return uploadResult.SecureUrl?.ToString() ??
               throw new Exception("Upload to Cloudinary failed.");
    }

    private async Task<string> CreateMediaObjectAsync(string igUserId, string caption, string mediaUrl, string accessToken, bool isVideo)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{igUserId}/media";

        var data = new Dictionary<string, string>
        {
            { isVideo ? "video_url" : "image_url", mediaUrl },
            { "caption", caption },
            { "access_token", accessToken }
        };

        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(data));
        var result = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(result);

        if (json.RootElement.TryGetProperty("id", out var id))
            return id.GetString()!;

        throw new Exception($"Failed to create media object: {result}");
    }

    private async Task<string> PublishMediaAsync(string igUserId, string creationId, string accessToken)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{igUserId}/media_publish";

        var data = new Dictionary<string, string>
        {
            { "creation_id", creationId },
            { "access_token", accessToken }
        };

        var response = await _httpClient.PostAsync(endpoint, new FormUrlEncodedContent(data));
        return await response.Content.ReadAsStringAsync();
    }
    private async Task<(string PostId, string Permalink)> FetchPublishedPostDetailsAsync(string mediaId, string accessToken)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{mediaId}?fields=id,permalink&access_token={accessToken}";

        var response = await _httpClient.GetAsync(endpoint);
        var jsonText = await response.Content.ReadAsStringAsync();

        var json = JsonDocument.Parse(jsonText);

        string? id = json.RootElement.GetProperty("id").GetString();
        string? link = json.RootElement.TryGetProperty("permalink", out var permalink)
            ? permalink.GetString()
            : null;

        return (id!, link ?? "");
    }
    public async Task<SocialPostResult> CreatePostAsync(string igUserId, string accessToken, string caption, List<IFormFile>? mediaFiles = null)
    {
        if (mediaFiles == null || mediaFiles.Count == 0)
        {
            return new SocialPostResult
            {
                Success = false,
                RawResponse = "Instagram requires at least one media file."
            };
        }

        try
        {
            var file = mediaFiles[0];

            string mediaUrl = await UploadToCloudinaryAsync(file);
            bool isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

            var creationId = await CreateMediaObjectAsync(igUserId, caption, mediaUrl, accessToken, isVideo);

            var publishResponse = await PublishMediaAsync(igUserId, creationId, accessToken);
            var publishJson = JsonDocument.Parse(publishResponse);

            if (!publishJson.RootElement.TryGetProperty("id", out var publishedIdJson))
            {
                return new SocialPostResult
                {
                    Success = false,
                    RawResponse = publishResponse
                };
            }

            var postId = publishedIdJson.GetString()!;

            var (pId, permalink) = await FetchPublishedPostDetailsAsync(postId, accessToken);

            return new SocialPostResult
            {
                Success = true,
                PostId = pId,
                Permalink = permalink,
                MediaUrls = new List<string> { mediaUrl },
                RawResponse = publishResponse
            };
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                RawResponse = ex.Message
            };
        }
    }
    public async Task<SocialPostResult> EditPostAsync(string igUserId, string accessToken, string oldMediaId, string newCaption, List<IFormFile>? newMedia = null)
    {
        try
        {
            await DeletePostAsync(accessToken, oldMediaId);

            return await CreatePostAsync(igUserId, accessToken, newCaption, newMedia);
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                RawResponse = ex.Message
            };
        }
    }
    public async Task<bool> DeletePostAsync(string accessToken, string mediaId)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{mediaId}?access_token={accessToken}";
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }
    public async Task<(IList<InstagramPostResponse>, string)> GetPostsAsync(string igUserId, string accessToken,string limit, int start = 100)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{igUserId}/media" +
                   $"?fields=id,caption,media_type,media_url,thumbnail_url,permalink,timestamp" +
                   $"&limit={limit}&access_token={accessToken}";

    if (!string.IsNullOrEmpty(limit))
        endpoint += $"&after={limit}"; // <-- Start index handling

    var response = await _httpClient.GetAsync(endpoint);
    var rawJson = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(rawJson);
    var posts = new List<InstagramPostResponse>();

    foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
    {
        var postId = element.GetProperty("id").GetString();

        // ----------------------------------------
        // 1️⃣ Fetch likes + comments
        // ----------------------------------------
        var detailEndpoint =
            $"https://graph.facebook.com/v21.0/{postId}" +
            $"?fields=like_count,comments_count&access_token={accessToken}";
        var detailJson = await _httpClient.GetStringAsync(detailEndpoint);
        var detailDoc = JsonDocument.Parse(detailJson);
        var detailRoot = detailDoc.RootElement;

        var likeCount = detailRoot.TryGetProperty("like_count", out var lc) ? lc.GetInt32() : 0;
        var commentCount = detailRoot.TryGetProperty("comments_count", out var cc) ? cc.GetInt32() : 0;
        int videoViews = 0;
        int impressions = 0;
        int reach = 0;

        var insightsEndpoint =
            $"https://graph.facebook.com/v21.0/{postId}/insights" +
            $"?metric=impressions,reach,video_views&access_token={accessToken}";
        var insightsJson = await _httpClient.GetStringAsync(insightsEndpoint);
        var insightsDoc = JsonDocument.Parse(insightsJson);

        if (insightsDoc.RootElement.TryGetProperty("data", out JsonElement insightsArray))
        {
            foreach (var metric in insightsArray.EnumerateArray())
            {
                var name = metric.GetProperty("name").GetString();
                var value = metric.GetProperty("values")[0].GetProperty("value").GetInt32();

                switch (name)
                {
                    case "impressions": impressions = value; break;
                    case "reach": reach = value; break;
                    case "video_views": videoViews = value; break;
                }
            }
        }

        var post = new InstagramPostResponse
        {
            Id = postId,
            Caption = element.TryGetProperty("caption", out var cap) ? cap.GetString() : null,
            Timestamp = element.TryGetProperty("timestamp", out var ts)
                        ? DateTime.Parse(ts.GetString())
                        : DateTime.MinValue,
            Media = new List<InstagramMediaItem>(),
            Likes = likeCount,
            Comments = commentCount,
            Views = videoViews,
            Impressions = impressions,
            Reach = reach
        };

        post.Media.Add(new InstagramMediaItem
        {
            Id = post.Id,
            MediaType = element.GetProperty("media_type").GetString(),
            MediaUrl = element.GetProperty("media_url").GetString(),
            ThumbnailUrl = element.TryGetProperty("thumbnail_url", out var thumb) ? thumb.GetString() : null
        });

        posts.Add(post);
    }
    string nextCursor = null;

    if (doc.RootElement.TryGetProperty("paging", out var paging) &&
        paging.TryGetProperty("cursors", out var cursors) &&
        cursors.TryGetProperty("after", out var after))
    {
        nextCursor = after.GetString();
    }

    return (posts.OrderByDescending(p => p.Timestamp).ToList(), nextCursor);
    }
    public async Task<PlatformStats> GetStats(string accessToken)
    {
        var userResp = await _httpClient.GetStringAsync($"https://graph.facebook.com/v18.0/me?fields=followers_count&access_token={accessToken}");
        var followers = JsonDocument.Parse(userResp).RootElement.GetProperty("followers_count").GetInt32();
        var mediaResp = await _httpClient.GetStringAsync($"https://graph.facebook.com/v18.0/me/media?fields=like_count,insights.metric(impressions)&access_token={accessToken}");
        var mediaJson = JsonDocument.Parse(mediaResp).RootElement.GetProperty("data");
        int totalLikes = 0;
        int totalImpressions = 0;
        foreach (var m in mediaJson.EnumerateArray())
        {
            if (m.TryGetProperty("like_count", out var like)) totalLikes += like.GetInt32();

            if (m.TryGetProperty("insights", out var insights))
            {
                foreach (var ins in insights.GetProperty("data").EnumerateArray())
                {
                    if (ins.TryGetProperty("name", out var name) &&
                        name.GetString() == "impressions")
                    {
                        totalImpressions += ins.GetProperty("values")[0]
                            .GetProperty("value").GetInt32();
                    }
                }
            }
        }
        return new PlatformStats
        {
            Followers = followers,
            Likes = totalLikes,
            Views = totalImpressions
        };
    }
}
