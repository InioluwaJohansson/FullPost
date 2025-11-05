using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tweetinvi.Models;

namespace FullPost.Interfaces.Services;
public interface ITwitterService
{
    public Task<ITweet> PostTweetAsync(string userAccessToken, string userAccessSecret, string message, List<IFormFile>? mediaFiles = null);

    Task<ITweet?> EditTweetAsync(string userAccessToken, string userAccessSecret, string tweetId, string newMessage, List<IFormFile>? newMedia = null);

    Task<bool> DeleteTweetAsync(string userAccessToken, string userAccessSecret, string tweetId);

    Task<IEnumerable<ITweet>> GetUserTweetsAsync(string userAccessToken, string userAccessSecret, int count = 5);
}