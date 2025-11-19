namespace FullPost.Models.DTOs;

public class CreateCustomerDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
public class CreateGoogleCustomerDto
{
    public string Email { get; set; }
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
    public IFormFile? PictureUrl { get; set; }
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
    public string TwitterUserName { get; set; }
    public string FacebookUserName { get; set; }
    public string InstagramUserName { get; set; }
    public string YouTubeUserName { get; set; }
    public string TikTokUserName { get; set; }
    public string LinkedInUserName { get; set; }
}
public class CustomerResponse : BaseResponse
{
    public GetCustomerDto Data { get; set; }
}