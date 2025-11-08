namespace FullPost.Models.DTOs;

public class CreatePostDto
{
    public int UserId { get; set; }
    public string Caption { get; set; }
    public List<IFormFile>? MediaFiles { get; set; }
    public List<string>? Platforms { get; set; }
}
public class EditPostDto
{
    public string PostId { get; set; }
    public int UserId { get; set; }
    public string NewCaption { get; set; }
    public List<IFormFile>? NewMediaFiles { get; set; }
}
public class GetPostDto
{
    public string Platform { get; set; }
    public string Id { get; set; }
    public string Text { get; set; }
    public string MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Permalink { get; set; }
}
public class PostsResponseModel : BaseResponse
{
    public new Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}