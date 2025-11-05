using FullPost.Contracts;

namespace FullPost.Entities;
public class Post : AuditableEntity{
    public string PostId { get; set; } = Guid.NewGuid().ToString();
    public int CustomerId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string MediaUrls { get; set; }
    public string TwitterPostId { get; set; }
    public string FacebookPostId { get; set; }
    public string InstagramPostId { get; set; }
    public bool IsDrafted { get; set; }
}