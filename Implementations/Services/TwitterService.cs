using Tweetinvi;
using Tweetinvi.Parameters;
using FullPost.Interfaces.Services;
using Tweetinvi.Models;
using FullPost.Models.DTOs;
namespace FullPost.Implementations.Services;
public class TwitterService: ITwitterService
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

    public async Task<ITweet> PostTweetAsync(string userAccessToken, string userAccessSecret, string message, List<IFormFile>? mediaFiles = null)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        List<IMedia>? uploadedMedia = null;

        try
        {
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
            return tweet ?? throw new Exception("Tweet could not be created.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting tweet: {ex.Message}");
            throw;
        }
    }

    public async Task<ITweet?> EditTweetAsync(string userAccessToken, string userAccessSecret, string tweetId, string newMessage, List<IFormFile>? newMedia = null)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);

        try
        {
            var oldTweet = await client.Tweets.GetTweetAsync(long.Parse(tweetId));
            if (oldTweet == null)
                throw new Exception("Tweet not found.");

            await client.Tweets.DestroyTweetAsync(long.Parse(tweetId));
            var newTweet = await PostTweetAsync(userAccessToken, userAccessSecret, newMessage, newMedia);
            return newTweet;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing tweet: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteTweetAsync(string userAccessToken, string userAccessSecret, string tweetId)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        try
        {
            await client.Tweets.DestroyTweetAsync(long.Parse(tweetId));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting tweet: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ITweet>> GetUserTweetsAsync(string userAccessToken, string userAccessSecret, int count = 5)
    {
        var client = CreateClient(userAccessToken, userAccessSecret);
        var user = await client.Users.GetAuthenticatedUserAsync();
        var timelineParams = new GetUserTimelineParameters(user)
        {
            PageSize = count
        };
        var tweets = await client.Timelines.GetUserTimelineAsync(timelineParams);
        return tweets;
    }
}