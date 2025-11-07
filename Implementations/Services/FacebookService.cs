using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using System.Text.Json;
namespace FullPost.Implementations.Services;
public class FacebookService : IFacebookService
{
    private readonly HttpClient _httpClient;
    public FacebookService()
    {
        _httpClient = new HttpClient();
    }
    public async Task<string> CreatePostAsync(string userAccessToken, string pageId, string message, List<IFormFile>? mediaFiles = null)
    {
        if (mediaFiles == null || mediaFiles.Count == 0)
        {
            var textUrl = $"https://graph.facebook.com/{pageId}/feed";
            var textData = new Dictionary<string, string>
            {
                { "message", message },
                { "access_token", userAccessToken }
            };
            var textResponse = await _httpClient.PostAsync(textUrl, new FormUrlEncodedContent(textData));
            var textContent = await textResponse.Content.ReadAsStringAsync();
            return textContent;
        }
        else
        {
            string lastMediaId = "";

            foreach (var file in mediaFiles)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var form = new MultipartFormDataContent();
                form.Add(new StringContent(message), "message");
                form.Add(new StringContent(userAccessToken), "access_token");
                form.Add(new ByteArrayContent(bytes)
                {
                    Headers =
                    {
                        ContentType = MediaTypeHeaderValue.Parse(file.ContentType)
                    }
                }, "source", file.FileName);

                var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
                var url = isVideo
                    ? $"https://graph.facebook.com/{pageId}/videos"
                    : $"https://graph.facebook.com/{pageId}/photos";

                var uploadResponse = await _httpClient.PostAsync(url, form);
                var content = await uploadResponse.Content.ReadAsStringAsync();
                lastMediaId = content;
            }

            return lastMediaId;
        }
    }

    public async Task<string?> EditPostAsync(string postId,string userAccessToken,string pageId,string newMessage,List<IFormFile>? newMediaFiles = null)
    {
        try
        {
            if (newMediaFiles == null || newMediaFiles.Count == 0)
            {
                var editUrl = $"https://graph.facebook.com/{postId}";
                var data = new Dictionary<string, string>
                {
                    { "message", newMessage },
                    { "access_token", userAccessToken }
                };

                var response = await _httpClient.PostAsync(editUrl, new FormUrlEncodedContent(data));
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return postId;

                return null;
            }

            var deleteResponse = await _httpClient.DeleteAsync($"https://graph.facebook.com/{postId}?access_token={userAccessToken}");
            if (!deleteResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var createResult = await CreatePostAsync(userAccessToken, pageId, newMessage, newMediaFiles);

            if (!string.IsNullOrWhiteSpace(createResult))
            {
                using var jsonDoc = JsonDocument.Parse(createResult);
                if (jsonDoc.RootElement.TryGetProperty("id", out var newPostId))
                    return newPostId.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeletePostAsync(string userAccessToken, string postId)
    {
        var url = $"https://graph.facebook.com/{postId}?access_token={userAccessToken}";
        var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetPostsAsync(string userAccessToken, string pageId, int limit = 5)
    {
        var url = $"https://graph.facebook.com/{pageId}/posts?limit={limit}&access_token={userAccessToken}";
        var response = await _httpClient.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
