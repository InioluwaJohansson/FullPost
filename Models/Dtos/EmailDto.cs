namespace FullPost.Models.DTOs;
public class CreateEmailDto
{
    public string ReceiverName { get; set; }
    public string ReceiverEmail { get; set; }
    public string Message { get; set; }
    public string Subject { get; set; }
    public IFormFile Attachment { get; set; }
    public string AttachmentUrl { get; set; }
}
public class GetEmailDto
{
    public string ReceiverName { get; set; }
    public string ReceiverEmail { get; set; }
    public string Message { get; set; }
    public string Subject { get; set; }
    public string AttachmentUrl { get; set; }
}
public class EmailResponseModel : BaseResponse
{
    public GetEmailDto Data { get; set; }
}
public class EmailsResponseModel : BaseResponse
{
    public ICollection<GetEmailDto> Data { get; set; } = new HashSet<GetEmailDto>();
}