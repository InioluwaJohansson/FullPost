using System.Text.Json;
namespace FullPost.Interfaces.Services;
public interface ILinkedInService
{
    Task<JsonElement> GetUserProfileAsync(string accessToken);
    Task<JsonElement> GetUserEmailAsync(string accessToken);
    Task<bool> CreatePostAsync(string accessToken, string linkedInUserId, string message, string? mediaUrl = null);
    Task<JsonElement> GetAllPostsAsync(string accessToken, string linkedInUserId);
    Task<bool> EditPostAsync(string accessToken, string postUrn, string newMessage);
    Task<bool> DeletePostAsync(string accessToken, string postUrn);
    Task<string> ExchangeCodeForTokenAsync(string code);
}
