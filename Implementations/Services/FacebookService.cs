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
using System.Text.Json;

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

    public async Task<IList<FacebookPostResponse>> GetPostsAsync(string pageId, string accessToken, int limit = 30)
    {
        var url = $"https://graph.facebook.com/{pageId}/posts?limit={limit}&access_token={accessToken}";
        var rawJson = await _httpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(rawJson);
        var posts = new List<FacebookPostResponse>();
        foreach (var postElement in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var post = new FacebookPostResponse
            {
                Id = postElement.GetProperty("id").GetString(),
                Message = postElement.TryGetProperty("message", out var msg) ? msg.GetString() : null,
                CreatedTime = postElement.TryGetProperty("created_time", out var ct)
                              ? DateTime.Parse(ct.GetString())
                              : DateTime.MinValue,
                Media = new List<FacebookMediaItem>()
            };
            if (postElement.TryGetProperty("attachments", out var attachments))
            {
                foreach (var attach in attachments.GetProperty("data").EnumerateArray())
                {
                    if (attach.TryGetProperty("subattachments", out var subattachments))
                    {
                        foreach (var sub in subattachments.GetProperty("data").EnumerateArray())
                        {
                            post.Media.Add(new FacebookMediaItem
                            {
                                MediaType = sub.GetProperty("media_type").GetString(),
                                MediaUrl = sub.GetProperty("media_url").GetString(),
                                ThumbnailUrl = sub.TryGetProperty("thumbnail_url", out var thumb) ? thumb.GetString() : null
                            });
                        }
                    }
                    else
                    {
                        post.Media.Add(new FacebookMediaItem
                        {
                            MediaType = attach.GetProperty("media_type").GetString(),
                            MediaUrl = attach.GetProperty("media_url").GetString(),
                            ThumbnailUrl = attach.TryGetProperty("thumbnail_url", out var thumb) ? thumb.GetString() : null
                        });
                    }
                }
            }

            posts.Add(post);
        }
        return posts.OrderByDescending(p => p.CreatedTime).ToList();
    }
}
