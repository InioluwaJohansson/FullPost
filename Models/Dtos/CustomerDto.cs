namespace FullPost.Models.DTOs;

public class CreateCustomerDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
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
    public string Email { get; set; }
    public IFormFile PictureUrl { get; set; }
    public string TwitterAccessToken { get; set; }
    public string TwitterAccessSecret { get; set; }
    public string TwitterUsername { get; set; }
    public string TwitterUserId { get; set; }
    public DateTime? TwitterTokenExpiry { get; set; }

    public string FacebookAccessToken { get; set; }
    public string FacebookPageId { get; set; }
    public string FacebookUserId { get; set; }
    public string FacebookPageName { get; set; }
    public DateTime? FacebookTokenExpiry { get; set; }

    public string InstagramAccessToken { get; set; }
    public string InstagramUserId { get; set; }
    public string InstagramUsername { get; set; }
    public DateTime? InstagramTokenExpiry { get; set; }
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
    public bool TwitterConnected { get; set; }
    public bool FacebookConnected { get; set; }
    public bool InstagramConnected { get; set; }
}
public class CustomerResponse : BaseResponse
{
    public GetCustomerDto Data { get; set; }
}