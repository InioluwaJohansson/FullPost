namespace FullPost.Models.DTOs;

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
    public ICollection<GetPostDto> Data { get; set; } = new HashSet<GetPostDto>();
}