using FullPost.Contracts;
using FullPost.Entities.Identity;
namespace FullPost.Entities;

public class Customer : AuditableEntity
{
    public User User { get; set; }
    public int UserId { get; set; }
    public string CustomerId { get; set; } = Guid.NewGuid().ToString().Substring(0, 17);
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PictureUrl { get; set; } = "";
    public string GoogleId { get; set; } = "";
    public string GoogleAccessToken { get; set; } = "";
    public string GoogleRefreshToken { get; set; } = "";
    public DateTime GoogleTokenExpiry { get; set; }

    public string TwitterAccessToken { get; set; } = "";
    public string TwitterAccessSecret { get; set; } = "";
    public string TwitterUsername { get; set; } = "";
    public string TwitterUserId { get; set; } = "";
    public DateTime TwitterTokenExpiry { get; set; }

    public string FacebookAccessToken { get; set; } = "";
    public string FacebookPageId { get; set; } = "";
    public string FacebookUserId { get; set; } = "";
    public string FacebookPageName { get; set; } = "";
    public DateTime FacebookTokenExpiry { get; set; }

    public string InstagramAccessToken { get; set; } = "";
    public string InstagramUserId { get; set; } = "";
    public string InstagramUsername { get; set; } = "";
    public DateTime InstagramTokenExpiry { get; set; }

    public string YouTubeAccessToken { get; set; } = "";
    public string YouTubeRefreshToken { get; set; } = "";
    public DateTime YouTubeTokenExpiry { get; set; }
    public string YouTubeChannelId { get; set; } = "";
    public string YouTubeChannelName { get; set; } = "";
    public string YouTubeEmail { get; set; } = "";
    public string YouTubeProfilePicture { get; set; } = "";

    public string TikTokAccessToken { get; set; } = "";
    public string TikTokRefreshToken { get; set; } = "";
    public DateTime TikTokTokenExpiry { get; set; }
    public string TikTokUserId { get; set; } = "";
    public string TikTokUsername { get; set; } = "";

    public string LinkedInAccessToken { get; set; } = "";
    public DateTime LinkedInTokenExpiry { get; set; }
    public string LinkedInUserId { get; set; } = "";
    public string LinkedInProfileUrl { get; set; } = "";
    public string LinkedInUsername { get; set; } = "";



}
