using System.Security.Cryptography;
using System.Text;
using FullPost.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FullPost.Controllers;

[ApiController]
[Route("/webhooks/paystack")]
public class PaystackWebhookController : Controller
{
    private readonly IConfiguration _config;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PaystackWebhookController> _logger;

    public PaystackWebhookController(IConfiguration config, ISubscriptionService subscriptionService, ILogger<PaystackWebhookController> logger)
    {
        _config = config;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        string json = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        string paystackSignature = Request.Headers["x-paystack-signature"];
        string secret = _config["Paystack:SecretKey"];
        if (!IsSignatureValid(json, secret, paystackSignature))
        {
            _logger.LogWarning("Invalid Paystack signature!");
            return Unauthorized();
        }
        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        string eventType = data.@event;
        _logger.LogInformation("Received Paystack Webhook: " + eventType);
        switch (eventType)
        {
            case "charge.success":
                await _subscriptionService.OnInitialSubscriptionPaid(data);
                return Ok(true);

            case "invoice.payment_success":
                await _subscriptionService.OnSubscriptionRenewed(data);
                return Ok(true);

            case "invoice.payment_failed":
                await _subscriptionService.OnSubscriptionPaymentFailed(data);
                return BadRequest(false);

            default:
                _logger.LogInformation("Unhandled webhook event: " + eventType);
                break;
        }
        return Ok();
    }
    private bool IsSignatureValid(string json, string secret, string receivedHash)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        string hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return hashString == receivedHash;
    }
}
