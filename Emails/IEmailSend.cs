using FullPost.Models.DTOs;

namespace FullPost.Emails;

public interface IEmailSend
{
    Task<bool> SendMail(CreateEmailDto createEmailDto);
}