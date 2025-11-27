using System.Collections.Generic;
using System.Threading.Tasks;
using FullPost.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Tweetinvi.Models;

namespace FullPost.Interfaces.Services;
public interface ITwitterService
{
    Task<SocialPostResult> PostTweetAsync(string userAccessToken, string userAccessSecret, string message, List<IFormFile>? mediaFiles = null);
    Task<SocialPostResult?> EditTweetAsync(string userAccessToken, string userAccessSecret, string tweetId, string newMessage, List<IFormFile>? newMedia = null);
    Task<bool> DeleteTweetAsync(string userAccessToken, string userAccessSecret, string tweetId);
    Task<IList<TwitterTweetResponse>> GetUserTweetsAsync(string userAccessToken, string userAccessSecret, long? sinceId = null, int count = 100);
    Task<PlatformStats> GetStats(string accessToken, string accessSecret, string bearerToken);
}