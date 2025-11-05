using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using FullPost.Interfaces.Services;
namespace FullPost.Implementations.Services;
public class InstagramService : IInstagramService
{
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly Cloudinary _cloudinary;
    private readonly HttpClient _httpClient;

    public InstagramService(string appId, string appSecret, string cloudName, string cloudApiKey, string cloudApiSecret)
    {
        _appId = appId;
        _appSecret = appSecret;
        var account = new Account(cloudName, cloudApiKey, cloudApiSecret);
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

        return uploadResult.SecureUrl?.ToString() ?? throw new Exception("Upload to Cloudinary failed.");
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

    public async Task<string> CreatePostAsync(string igUserId, string accessToken, string caption, List<IFormFile>? mediaFiles = null)
    {
        if (mediaFiles == null || mediaFiles.Count == 0)
            throw new Exception("Instagram requires at least one media file.");

        var file = mediaFiles[0];

        string mediaUrl = await UploadToCloudinaryAsync(file);
        bool isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        var creationId = await CreateMediaObjectAsync(igUserId, caption, mediaUrl, accessToken, isVideo);
        var publishResult = await PublishMediaAsync(igUserId, creationId, accessToken);

        return publishResult;
    }

    public async Task<string> EditPostAsync(string igUserId, string accessToken, string oldMediaId, string newCaption, List<IFormFile>? newMedia = null)
    {
        Console.WriteLine($"Editing post {oldMediaId}...");
        await DeletePostAsync(accessToken, oldMediaId);
        return await CreatePostAsync(igUserId, accessToken, newCaption, newMedia);
    }

    public async Task<bool> DeletePostAsync(string accessToken, string mediaId)
    {
        var endpoint = $"https://graph.facebook.com/v21.0/{mediaId}?access_token={accessToken}";
        var response = await _httpClient.DeleteAsync(endpoint);
        Console.WriteLine(response.IsSuccessStatusCode
            ? $"Deleted post {mediaId} successfully."
            : $"Failed to delete post: {await response.Content.ReadAsStringAsync()}");
        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetPostsAsync(string igUserId, string accessToken, int limit = 5)
    {
        var endpoint =
            $"https://graph.facebook.com/v21.0/{igUserId}/media?fields=id,caption,media_type,media_url,permalink,timestamp&limit={limit}&access_token={accessToken}";
        var response = await _httpClient.GetAsync(endpoint);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Retrieved {limit} posts for {igUserId}");
        return result;
    }
}