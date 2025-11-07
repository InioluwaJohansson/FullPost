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
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string AppPassword { get; set; }
}
public class BaseResponse
{
    public string Message { get; set; }
    public bool Status { get; set; }
}

