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

        // Upload media
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

        //Fetch tweet again to get media URLs
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

    public async Task<IEnumerable<SocialPostResult>> GetUserTweetsAsync(string userAccessToken, string userAccessSecret, int count = 5)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        var user = await client.Users.GetAuthenticatedUserAsync();

        var timelineParams = new GetUserTimelineParameters(user)
        {
            PageSize = count
        };

        var tweets = await client.Timelines.GetUserTimelineAsync(timelineParams);

        return tweets.Select(t => new SocialPostResult
        {
            Success = true,
            PostId = t.IdStr,
            MediaUrls = t.Media?.Count > 0 ? t.Media.Select(m => m.MediaURLHttps).ToList() : null,
            Permalink = $"https://twitter.com/{t.CreatedBy.ScreenName}/status/{t.IdStr}",
            RawResponse = t.FullText
        });
    }
}