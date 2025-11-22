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
    public async Task<IList<InstagramPostResponse>> GetPostsAsync(string igUserId, string accessToken, int limit = 30)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{igUserId}/media?fields=id,caption,media_type,media_url,permalink,timestamp&limit={limit}&access_token={accessToken}";
        var response = await _httpClient.GetAsync(endpoint);
        var rawJson = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(rawJson);
        var posts = new List<InstagramPostResponse>();

        foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var post = new InstagramPostResponse
            {
                Id = element.GetProperty("id").GetString(),
                Caption = element.TryGetProperty("caption", out var cap) ? cap.GetString() : null,
                Timestamp = element.TryGetProperty("timestamp", out var ts) ? DateTime.Parse(ts.GetString()) : DateTime.MinValue,
                Media = new List<InstagramMediaItem>()
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
        return posts.OrderByDescending(p => p.Timestamp).ToList();
    }
}
