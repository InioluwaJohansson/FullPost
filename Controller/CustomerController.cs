using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FullPost.Implementations.Services;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using System.Text.Json;
using Google.Apis.Auth;

namespace FullPost.Controllers;

[Route("FullPost/[controller]")]
[ApiController]
public class CustomerController : Controller
{
    ICustomerService _customerService;
    private readonly IConfiguration _config;
    public CustomerController(ICustomerService customerService, IConfiguration config)
    {
        _customerService = customerService;
        _config = config;
    }
    [HttpGet("google/signup")]
    public IActionResult GoogleSignUp()
    {
        var clientId = _config["Google:ClientId"];
        var redirectUri = _config["Google:RedirectUriSignup"];
        var scope = "openid profile email";
        var googleAuthUrl =
            $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}" +
            $"&redirect_uri={redirectUri}" +
            $"&response_type=code&scope={scope}&access_type=offline&prompt=consent";
        return Redirect(googleAuthUrl);
    }

    [HttpGet("google/signup/callback")]
    public async Task<IActionResult> GoogleSignUpCallback(string code)
    {
        var clientId = _config["Google:ClientId"];
        var clientSecret = _config["Google:ClientSecret"];
        var redirectUri = _config["Google:RedirectUriSignup"];
        var tokenUrl = "https://oauth2.googleapis.com/token";

        using var client = new HttpClient();
        var data = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };
        var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(data));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest($"Google signup token exchange failed: {json}");

        var tokenData = JsonDocument.Parse(json).RootElement;
        var idToken = tokenData.GetProperty("id_token").GetString();
        var accessToken = tokenData.GetProperty("access_token").GetString();
        var refreshToken = tokenData.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });
        var customer = new CreateCustomerDto
        {
            Email = payload.Email,
            FirstName = payload.Name,
            PictureUrl = payload.Picture,
            GoogleId = payload.Subject,
            GoogleAccessToken = accessToken,
            GoogleRefreshToken = refreshToken,
            GoogleTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        var baseResponse = await _customerService.CreateCustomerWithGoogle(customer);
        if (baseResponse.Status == false)
        {
            return Redirect($"{_config["App:FrontendUrl"]}/signup?error=already_exists");
        }
        return Ok(baseResponse);
    }
    [HttpPost("CreateCustomer")]
    public async Task<IActionResult> CreateCustomer([FromForm] CreateCustomerDto createCustomerDto)
    {
        var customer = await _customerService.CreateCustomer(createCustomerDto);
        if (customer.Status == true)
        {
            return Ok(customer);
        }
        return BadRequest(customer);
    }

    [HttpPut("UpdateCustomer")]
    public async Task<IActionResult> UpdateCustomer([FromForm] UpdateCustomerDto updateCustomerDto)
    {
        var customer = await _customerService.UpdateCustomer(updateCustomerDto);
        if (customer.Status == true)
        {
            return Ok(customer);
        }
        return BadRequest(customer);
    }

    [HttpGet("GetCustomerById")]
    public async Task<IActionResult> GetCustomerById(int userId)
    {
        var customer = await _customerService.GetCustomerById(userId);
        if (customer.Status == true)
        {
            return Ok(customer);
        }
        return BadRequest(customer);
    }
    [HttpPut("DeleteAccount")]
    public async Task<IActionResult> DeleteAccount(int userId)
    {
        var customer = await _customerService.DeleteAccount(userId);
        if (customer.Status == true)
        {
            return Ok(customer);
        }
        return BadRequest(customer);
    }
}
