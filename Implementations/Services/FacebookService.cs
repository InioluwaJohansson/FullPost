using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;

namespace FullPost.Implementations.Services;

public class FacebookService : IFacebookService
{
    private readonly HttpClient _httpClient;

    public FacebookService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<SocialPostResult> CreatePostAsync(string pageId, string accessToken, string message, List<IFormFile>? mediaFiles = null)
    {
        var mediaUrls = new List<string>();
        string? postId = null;
        string? permalink = null;

        try
        {
            // TEXT POST ONLY --------------------------
            if (mediaFiles == null || mediaFiles.Count == 0)
            {
                var url = $"https://graph.facebook.com/{pageId}/feed";
                var data = new Dictionary<string, string>
                {
                    { "message", message },
                    { "access_token", accessToken }
                };

                var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(data));
                var content = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(content);

                postId = json["id"]?.ToString();
                permalink = postId != null ? $"https://facebook.com/{postId}" : null;

                return new SocialPostResult
                {
                    Success = response.IsSuccessStatusCode,
                    PostId = postId,
                    Permalink = permalink,
                    MediaUrls = mediaUrls,
                    RawResponse = content
                };
            }

            // MEDIA POST -------------------------------
            string? lastMediaPostId = null;

            foreach (var file in mediaFiles)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var form = new MultipartFormDataContent
                {
                    { new StringContent(message), "message" },
                    { new StringContent(accessToken), "access_token" }
                };

                var fileContent = new ByteArrayContent(ms.ToArray());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);

                form.Add(fileContent, "source", file.FileName);

                var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
                var uploadUrl = isVideo
                    ? $"https://graph.facebook.com/{pageId}/videos"
                    : $"https://graph.facebook.com/{pageId}/photos";

                var uploadResponse = await _httpClient.PostAsync(uploadUrl, form);
                var uploadText = await uploadResponse.Content.ReadAsStringAsync();

                var json = JObject.Parse(uploadText);

                var mediaId = json["id"]?.ToString();
                lastMediaPostId = json["post_id"]?.ToString() ?? lastMediaPostId;

                if (!string.IsNullOrEmpty(mediaId))
                {
                    // Fetch URL of the uploaded media
                    var mediaInfoUrl = isVideo
                        ? $"https://graph.facebook.com/{mediaId}?fields=permalink_url,source&access_token={accessToken}"
                        : $"https://graph.facebook.com/{mediaId}?fields=images,link&access_token={accessToken}";

                    var mediaInfo = await _httpClient.GetStringAsync(mediaInfoUrl);
                    var mediaJson = JObject.Parse(mediaInfo);

                    if (isVideo)
                        mediaUrls.Add(mediaJson["permalink_url"]?.ToString() ?? "");
                    else
                        mediaUrls.Add(mediaJson["link"]?.ToString() ?? "");
                }
            }

            postId = lastMediaPostId;
            permalink = postId != null ? $"https://facebook.com/{postId}" : null;

            return new SocialPostResult
            {
                Success = true,
                PostId = postId,
                Permalink = permalink,
                MediaUrls = mediaUrls,
                RawResponse = ""
            };
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                PostId = postId,
                Permalink = permalink,
                MediaUrls = mediaUrls,
                RawResponse = ex.Message
            };
        }
    }

    public async Task<SocialPostResult> EditPostAsync(string pageId, string accessToken, string postId, string newMessage, List<IFormFile>? newMedia = null)
    {
        try
        {
            // If no media change → just edit message
            if (newMedia == null || newMedia.Count == 0)
            {
                var url = $"https://graph.facebook.com/{postId}";
                var data = new Dictionary<string, string>
                {
                    { "message", newMessage },
                    { "access_token", accessToken }
                };

                var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(data));
                var content = await response.Content.ReadAsStringAsync();

                return new SocialPostResult
                {
                    Success = response.IsSuccessStatusCode,
                    PostId = postId,
                    Permalink = $"https://facebook.com/{postId}",
                    MediaUrls = null,
                    RawResponse = content
                };
            }

            // Facebook does NOT support editing media → delete + recreate
            await DeletePostAsync(accessToken, postId);

            return await CreatePostAsync(pageId, accessToken, newMessage, newMedia);
        }
        catch (Exception ex)
        {
            return new SocialPostResult
            {
                Success = false,
                PostId = postId,
                RawResponse = ex.Message
            };
        }
    }

    public async Task<bool> DeletePostAsync(string accessToken, string postId)
    {
        var url = $"https://graph.facebook.com/{postId}?access_token={accessToken}";
        var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetPostsAsync(string pageId, string accessToken, int limit = 5)
    {
        var url = $"https://graph.facebook.com/{pageId}/posts?limit={limit}&access_token={accessToken}";
        return await _httpClient.GetStringAsync(url);
    }
}
