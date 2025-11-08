using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FullPost.Interfaces.Services;
using System.Text.Json;
using Google.Apis.Auth;
using FullPost.Models.DTOs;
using FullPost.Authentication;

namespace FullPost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;
    private readonly IJWTAuthentication _jwtAuthentication;

    public AuthController(IUserService userService, IConfiguration config, IJWTAuthentication jwtAuthentication)
    {
        _userService = userService;
        _config = config;
        _jwtAuthentication = jwtAuthentication;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var login = await _userService.Login(email, password);
        if (login.Status == true)
        {
            login.Token = _jwtAuthentication.GenerateToken(login);
            return Ok(login);
        }
        return BadRequest(login);
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        var clientId = _config["Google:ClientId"];
        var redirectUri = _config["Google:RedirectUriLogin"];
        var scope = "openid profile email";

        var googleAuthUrl =
            $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}" +
            $"&redirect_uri={redirectUri}" +
            $"&response_type=code&scope={scope}&access_type=offline&prompt=consent";

        return Redirect(googleAuthUrl);
    }

    [HttpGet("google/login/callback")]
    public async Task<IActionResult> GoogleLoginCallback(string code)
    {
        var clientId = _config["Google:ClientId"];
        var clientSecret = _config["Google:ClientSecret"];
        var redirectUri = _config["Google:RedirectUriLogin"];
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
            return BadRequest($"Google login token exchange failed: {json}");

        var tokenData = JsonDocument.Parse(json).RootElement;
        var idToken = tokenData.GetProperty("id_token").GetString();
        var accessToken = tokenData.GetProperty("access_token").GetString();

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });

        var googleLoginRequest = new GoogleLoginRequest
        {
            Email = payload.Email,
            GoogleAccessToken = accessToken!,
            GoogleTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        var loginResponse = await _userService.GoogleLoginAsync(googleLoginRequest);
        if (loginResponse.Status == false)
        {
            return Redirect($"{_config["App:FrontendUrl"]}/login?error=user_not_found");
        }
        loginResponse.Token = _jwtAuthentication.GenerateToken(loginResponse);
        return Ok(loginResponse);
    }
    [HttpGet("passwordReset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var resetResponse = await _userService.ResetPassword(request);
        if (resetResponse.Status == true)
        {
            return Ok(resetResponse);
        }
        return BadRequest(resetResponse);
    }
    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var forgotResponse = await _userService.ForgotPassword(email);
        if (forgotResponse.Status == true)
        {
            return Ok(forgotResponse);
        }
        return BadRequest(forgotResponse);
    }
}