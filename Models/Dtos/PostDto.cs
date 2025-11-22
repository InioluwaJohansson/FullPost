namespace FullPost.Models.DTOs;

public class CreatePostDto
{
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Caption { get; set; }
    public List<IFormFile>? MediaFiles { get; set; }
    public List<string>? Platforms { get; set; }
}
public class EditPostDto
{
    public string PostId { get; set; }
    public int UserId { get; set; }
    public string NewTitle { get; set; }
    public string NewCaption { get; set; }
    public List<IFormFile>? NewMediaFiles { get; set; }
    public List<string>? Platforms { get; set; }
}
public class GetPostDto
{
    public List<string> Platform { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    public string Caption { get; set; }
    public List<string> MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TwitterPostLink { get; set; }
    public string FacebookPostLink { get; set; }
    public string InstagramPostLink { get; set; }
    public string YouTubePostLink { get; set; }
    public string TikTokPostLink { get; set; }
    public string LinkedInPostLink { get; set; }
}
public class PostsResponseModel : BaseResponse
{
    public new ICollection<GetPostDto> Data { get; set; } = new HashSet<GetPostDto>();
}