namespace FullPost.Models.DTOs;

public class LoginResponse : BaseResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
}