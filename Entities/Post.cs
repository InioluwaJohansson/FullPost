using FullPost.Contracts;

namespace FullPost.Entities;
public class Post : AuditableEntity
{
    public string PostId { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string MediaUrls { get; set; }
    public string TwitterPostId { get; set; }
    public string FacebookPostId { get; set; }
    public string InstagramPostId { get; set; }
    public string YouTubePostId { get; set; }
    public string TikTokPostId { get; set; }
    public string LinkedInPostId { get; set; }
}