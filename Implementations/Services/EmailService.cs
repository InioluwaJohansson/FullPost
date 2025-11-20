using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;
using MailKit.Net.Smtp;
namespace FullPost.Implementations.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task<bool> SendEmailAsync(string to, string subject,string body, bool isHtml = true)
    {
        var apiKey = _config["Twillo:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress("fullpost10@gmail.com", _config["App:Name"]);
        var emailSubject = subject;
        var emailTo = new EmailAddress(to, "User");
        var plainTextContent = body;
        var htmlContent = "<strong>" + body + "</strong>";
        var msg = MailHelper.CreateSingleEmail(from, emailTo, emailSubject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
        Console.WriteLine("Email Response:");
        Console.WriteLine(response.StatusCode);
        Console.WriteLine(await response.Body.ReadAsStringAsync());
        Console.WriteLine(response.ToString());
        if(response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }
        return false;
    }
    public async Task<bool> SendEmailAsyncGmail(string to, string subject, string body, bool isHtml = true)
    {
        var clientId = _config["Google:ClientId"];
        var clientSecret = _config["Google:ClientSecret"];
        var refreshToken = _config["Google:RefreshToken"];

        var credential = GoogleCredential.FromAccessToken(refreshToken)
            .CreateScoped(GmailService.Scope.GmailSend)
            .UnderlyingCredential as UserCredential;

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "FullPost"
        });

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FullPost", "inioluwa.makinde10@gmail.com"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var ms = new MemoryStream();
        await message.WriteToAsync(ms);
        var encodedMessage = Convert.ToBase64String(ms.ToArray())
            .Replace('+', '-').Replace('/', '_').Replace("=", "");

        var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
        {
            Raw = encodedMessage
        };

        await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
        return true;
    }
}
