using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace FullPost.Implementations.Services;
public class TwitterService : ITwitterService
{
    private readonly TwitterKeys _twitterKeys;

    public TwitterService(IConfiguration config)
    {
        _twitterKeys = config.GetSection("TwitterKeys").Get<TwitterKeys>();
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
}