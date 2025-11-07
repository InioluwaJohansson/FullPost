using System.Text.Json;
namespace FullPost.Interfaces.Services;
public interface ILinkedInService
{
    Task<JsonElement> GetUserProfileAsync(string accessToken);
    Task<JsonElement> GetUserEmailAsync(string accessToken);
    Task<bool> PostContentAsync(string accessToken, string linkedInUserId, string message, string? mediaUrl = null);
    Task<string> ExchangeCodeForTokenAsync(string code);
}
