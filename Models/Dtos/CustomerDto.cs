namespace FullPost.Models.DTOs;

public class CreateCustomerDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PictureUrl { get; set; }
    public string GoogleId { get; set; }
    public string GoogleAccessToken { get; set; }
    public string GoogleRefreshToken { get; set; }
    public DateTime GoogleTokenExpiry { get; set; }
}
public class UpdateCustomerDto
{
    public int userId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public bool AutoSubscribe { get; set; }
    public IFormFile PictureUrl { get; set; }
}
public class GetCustomerDto
{
    public int Id { get; set; }
    public int userId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PictureUrl { get; set; }
    public bool GoogleConnected { get; set; }
    public bool TwitterConnected { get; set; }
    public bool FacebookConnected { get; set; }
    public bool InstagramConnected { get; set; }
    public bool YouTubeConnected { get; set; }
    public bool TikTokConnected { get; set; }
    public bool LinkedInConnected { get; set; }
}
public class CustomerResponse : BaseResponse
{
    public GetCustomerDto Data { get; set; }
}