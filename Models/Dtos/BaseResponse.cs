namespace FullPost.Models.DTOs;
public class CloudinarySettings
{
    public string CloudName { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}
public class BaseResponse
{
    public string Message { get; set; }
    public bool Status { get; set; }
}

