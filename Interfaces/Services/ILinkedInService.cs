using System.Text.Json;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface ILinkedInService
{
    Task<JsonElement> GetUserProfileAsync(string accessToken);
    Task<JsonElement> GetUserEmailAsync(string accessToken);
    Task<SocialPostResult> CreatePostAsync(string accessToken, string linkedInUserId, string message, string? mediaUrl = null);
    Task<IList<LinkedInPostResponse>> GetAllPostsAsync(string accessToken, string linkedInUserId);
    Task<SocialPostResult> EditPostAsync(string accessToken, string postUrn, string linkedInUserId, string newMessage, string? mediaUrl = null);
    Task<bool> DeletePostAsync(string accessToken, string postUrn);
    Task<string> ExchangeCodeForTokenAsync(string code);
}
