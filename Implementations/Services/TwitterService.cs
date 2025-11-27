using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FullPost.Implementations.Services;
public class TwitterService : ITwitterService
{
    private readonly TwitterKeys _twitterKeys;
    private readonly HttpClient _httpClient;
    public TwitterService(IConfiguration config)
    {
        _twitterKeys = config.GetSection("TwitterKeys").Get<TwitterKeys>();
        _httpClient = new HttpClient();
    }

    private TwitterClient CreateClient(string userAccessToken, string userAccessSecret)
    {
        return new TwitterClient(_twitterKeys.ApiKey, _twitterKeys.ApiSecret, userAccessToken, userAccessSecret);
    }

    public async Task<SocialPostResult> PostTweetAsync(string userAccessToken,string userAccessSecret,string message,List<IFormFile>? mediaFiles = null)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);

        List<IMedia>? uploadedMedia = null;

        if (mediaFiles != null && mediaFiles.Count > 0)
        {
            uploadedMedia = new List<IMedia>();

            foreach (var file in mediaFiles)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
                IMedia media = isVideo
                    ? await client.Upload.UploadTweetVideoAsync(bytes)
                    : await client.Upload.UploadTweetImageAsync(bytes);

                uploadedMedia.Add(media);
            }
        }

        var publishParams = new PublishTweetParameters(message)
        {
            Medias = uploadedMedia
        };

        var tweet = await client.Tweets.PublishTweetAsync(publishParams);
        if (tweet == null)
            throw new Exception("Tweet could not be created.");

    //  var tweetFetched = await client.Tweets.GetTweetAsync(tweet.Id);

    //  var mediaUrls = tweetFetched?.ExtendedEntities?.Media?.Select(m => m.MediaURLHttps).ToList();

        return new SocialPostResult
        {
            Success = true,
            PostId = tweet.IdStr,
            //MediaUrls = mediaUrls,
            Permalink = $"https://twitter.com/{tweet.CreatedBy.ScreenName}/status/{tweet.IdStr}"
        };
    }
    public async Task<SocialPostResult?> EditTweetAsync(string userAccessToken, string userAccessSecret, string tweetId, string newMessage, List<IFormFile>? newMedia = null)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        var oldTweet = await client.Tweets.GetTweetAsync(long.Parse(tweetId));
        if (oldTweet == null)
            return null;

        await client.Tweets.DestroyTweetAsync(long.Parse(tweetId));
        return await PostTweetAsync(userAccessToken, userAccessSecret, newMessage, newMedia);
    }
    public async Task<bool> DeleteTweetAsync(string userAccessToken, string userAccessSecret, string tweetId)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        try
        {
            await client.Tweets.DestroyTweetAsync(long.Parse(tweetId));
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<IList<TwitterTweetResponse>> GetUserTweetsAsync(string userAccessToken, string userAccessSecret, long? sinceId = null,  int count = 100)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        var user = await client.Users.GetAuthenticatedUserAsync();
        long? maxId = 2000;
        var timelineParams = new GetUserTimelineParameters(user)
        {
            PageSize = count,
            SinceId = sinceId ?? 0,
            MaxId = maxId
        };
        var tweets = await client.Timelines.GetUserTimelineAsync(timelineParams);
        var result = tweets.Select(t => new TwitterTweetResponse
        {
            Id = t.IdStr,
            Text = t.FullText,
            CreatedAt = t.CreatedAt.DateTime,
            Media = new TwitterMedia
            {
                Photos = t.Media?.Where(m => m.MediaType == "photo").Select(m => m.MediaURLHttps).ToList(),
                Videos = t.Media?.Where(m => m.MediaType == "video").Select(m => m.MediaURLHttps).ToList()
            },
            User = new TwitterUser
            {
                Username = t.CreatedBy.ScreenName,
                Name = t.CreatedBy.Name,
                ProfileImageUrl = t.CreatedBy.ProfileImageUrl
            },
            Likes = t.FavoriteCount,
            Comments = t.RetweetCount
        }).OrderByDescending(t => t.CreatedAt).ToList();
        return result;
    }
    public async Task<PlatformStats> GetStats(string accessToken, string accessSecret, string bearerToken)
    {
        var client = CreateClient(accessToken, accessSecret);

        var user = await client.Users.GetAuthenticatedUserAsync();
        var tweets = await client.Timelines.GetUserTimelineAsync(
            new GetUserTimelineParameters(user.Id)
            {
                PageSize = int.MaxValue
            }
        );

        if (tweets == null)
        {
            return new PlatformStats
            {
                Followers = user.FollowersCount,
                Views = 0,
                Likes = 0
            };
        }
        var tweetIds = tweets.Select(t => t.IdStr).ToList();
        int totalLikes = 0;
        int totalViews = 0;

        foreach (var chunk in tweetIds.Chunk(100))
        {
            var ids = string.Join(",", chunk);
            var url = $"https://api.twitter.com/2/tweets?ids={ids}&tweet.fields=public_metrics";
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                continue;

            foreach (var item in data.EnumerateArray())
            {
                var m = item.GetProperty("public_metrics");
                totalLikes += m.GetProperty("like_count").GetInt32();
                totalViews += m.GetProperty("impression_count").GetInt32();
            }
        }
        return new PlatformStats
        {
            Followers = user.FollowersCount,
            Views = totalViews,
            Likes = totalLikes
        };
    }


}