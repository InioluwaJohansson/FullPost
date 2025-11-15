namespace FullPost.Models.DTOs;

public class CloudinarySettings
{
    public string CloudName { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}
public class TwitterKeys
{
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}
public class SocialPostResult
{
    public bool Success { get; set; }
    public string? PostId { get; set; }
    public string? Permalink { get; set; }
    public List<string>? MediaUrls { get; set; }
    public string? RawResponse { get; set; }
}
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string AppPassword { get; set; }
}
public class GoogleLoginRequest
{
    public string Email { get; set; }
    public string GoogleAccessToken { get; set; }
    public DateTime GoogleTokenExpiry { get; set; }
}
public class ResetPasswordRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
public class BaseResponse
{
    public int UserId { get; set; }
    public string Message { get; set; }
    public bool Status { get; set; }
}

