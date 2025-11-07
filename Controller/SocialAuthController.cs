using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Models;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using Tweetinvi.Credentials.Models;

namespace FullPost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ICustomerRepo _customerRepository;
    private readonly HttpClient _httpClient;

    public SocialAuthController(IConfiguration config, ICustomerRepo customerRepository)
    {
        _config = config;
        _customerRepository = customerRepository;
        _httpClient = new HttpClient();
    }

    [HttpGet("twitter/connect")]
    public async Task<IActionResult> ConnectTwitter()
    {
        var client = new TwitterClient(_config["Twitter:ApiKey"], _config["Twitter:ApiSecret"]);
        var redirectUrl = $"{_config["App:BaseUrl"]}/api/socialauth/twitter/callback";
        var authenticationRequest = await client.Auth.RequestAuthenticationUrlAsync(redirectUrl);
        HttpContext.Session.SetString("TwitterAuthRequest", authenticationRequest.AuthorizationKey);
        HttpContext.Session.SetString("TwitterAuthRequestObject", System.Text.Json.JsonSerializer.Serialize(authenticationRequest));

        return Redirect(authenticationRequest.AuthorizationURL);
    }

    [HttpGet("twitter/callback")]
    public async Task<IActionResult> TwitterCallback(string oauth_verifier, int userId)
    {
        var authRequestJson = HttpContext.Session.GetString("TwitterAuthRequestObject");
        if (string.IsNullOrEmpty(authRequestJson))
            return BadRequest("Missing or expired Twitter auth session.");
        var authenticationRequest = System.Text.Json.JsonSerializer.Deserialize<AuthenticationRequest>(authRequestJson);
        var appClient = new TwitterClient(_config["Twitter:ApiKey"], _config["Twitter:ApiSecret"]);
        var userCredentials = await appClient.Auth.RequestCredentialsFromVerifierCodeAsync(oauth_verifier, authenticationRequest);
        var userClient = new TwitterClient(_config["Twitter:ApiKey"], _config["Twitter:ApiSecret"], userCredentials.AccessToken, userCredentials.AccessTokenSecret);
        var twitterUser = await userClient.Users.GetAuthenticatedUserAsync();
        var customer = await _customerRepository.Get(x => x.UserId == userId);
        if (customer == null)
            return BadRequest("Customer not found.");
        customer.TwitterAccessToken = userCredentials.AccessToken;
        customer.TwitterAccessSecret = userCredentials.AccessTokenSecret;
        customer.TwitterUserId = twitterUser.IdStr;
        customer.TwitterUsername = twitterUser.ScreenName;
        customer.TwitterTokenExpiry = DateTime.UtcNow.AddYears(5);

        await _customerRepository.Update(customer);
        return Redirect($"{_config["App:FrontendUrl"]}/dashboard?connected=twitter");
    }

    [HttpGet("facebook/connect")]
    public IActionResult ConnectFacebook()
    {
        var appId = _config["Facebook:AppId"];
        var redirectUri = $"{_config["App:BaseUrl"]}/api/socialauth/facebook/callback";
        var scope = "pages_manage_posts,pages_read_engagement,pages_show_list,instagram_basic,instagram_content_publish";
        var authUrl = $"https://www.facebook.com/v21.0/dialog/oauth?client_id={appId}&redirect_uri={redirectUri}&scope={scope}&response_type=code";
        return Redirect(authUrl);
    }

    [HttpGet("facebook/callback")]
    public async Task<IActionResult> FacebookCallback(string code, int userId)
    {
        var appId = _config["Facebook:AppId"];
        var appSecret = _config["Facebook:AppSecret"];
        var redirectUri = $"{_config["App:BaseUrl"]}/api/socialauth/facebook/callback";

        var tokenUrl = $"https://graph.facebook.com/v21.0/oauth/access_token?client_id={appId}&redirect_uri={redirectUri}&client_secret={appSecret}&code={code}";
        var response = await _httpClient.GetStringAsync(tokenUrl);
        var json = JsonDocument.Parse(response);
        var accessToken = json.RootElement.GetProperty("access_token").GetString();

        var longLivedUrl = $"https://graph.facebook.com/v21.0/oauth/access_token?grant_type=fb_exchange_token&client_id={appId}&client_secret={appSecret}&fb_exchange_token={accessToken}";
        var longResponse = await _httpClient.GetStringAsync(longLivedUrl);
        var longJson = JsonDocument.Parse(longResponse);
        var longToken = longJson.RootElement.GetProperty("access_token").GetString();
        var expirySeconds = longJson.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 5184000; // default 60 days

        var customer = await _customerRepository.Get(x => x.UserId == userId);
        if (customer == null)
            return BadRequest("Customer not found.");

        var pageResponse = await _httpClient.GetStringAsync($"https://graph.facebook.com/v21.0/me/accounts?access_token={longToken}");
        var pageJson = JsonDocument.Parse(pageResponse);
        var pages = pageJson.RootElement.GetProperty("data");
        if (pages.GetArrayLength() > 0)
        {
            var page = pages[0];
            customer.FacebookPageId = page.GetProperty("id").GetString();
            customer.FacebookPageName = page.GetProperty("name").GetString();
            customer.FacebookUserId = page.GetProperty("id").GetString();
        }

        customer.FacebookAccessToken = longToken;
        customer.FacebookTokenExpiry = DateTime.UtcNow.AddSeconds(expirySeconds);

        await _customerRepository.Update(customer);
        return Redirect($"{_config["App:FrontendUrl"]}/dashboard?connected=facebook");
    }

    [HttpGet("instagram/connect")]
    public IActionResult ConnectInstagram()
    {
        var appId = _config["Instagram:AppId"];
        var redirectUri = $"{_config["App:BaseUrl"]}/api/socialauth/instagram/callback";
        var scope = "instagram_basic,instagram_content_publish";

        var authUrl = $"https://api.instagram.com/oauth/authorize?client_id={appId}&redirect_uri={redirectUri}&scope={scope}&response_type=code";
        return Redirect(authUrl);
    }

    [HttpGet("instagram/callback")]
    public async Task<IActionResult> InstagramCallback(string code, int userId)
    {
        var appId = _config["Instagram:AppId"];
        var appSecret = _config["Instagram:AppSecret"];
        var redirectUri = $"{_config["App:BaseUrl"]}/api/socialauth/instagram/callback";

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", appId },
            { "client_secret", appSecret },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri },
            { "code", code }
        });

        var response = await _httpClient.PostAsync("https://api.instagram.com/oauth/access_token", form);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = json.RootElement.GetProperty("access_token").GetString();
        var userIdStr = json.RootElement.GetProperty("user_id").GetInt64().ToString();

        var customer = await _customerRepository.Get(x => x.UserId == userId);
        if (customer == null)
            return BadRequest("Customer not found.");

        customer.InstagramAccessToken = accessToken;
        customer.InstagramUserId = userIdStr;
        customer.InstagramTokenExpiry = DateTime.UtcNow.AddDays(60);
        customer.InstagramUsername = $"user_{userIdStr}";

        await _customerRepository.Update(customer);
        return Redirect($"{_config["App:FrontendUrl"]}/dashboard?connected=instagram");
    }
    [HttpGet("youtube/connect")]
    public IActionResult ConnectYouTube()
    {
        var clientId = _config["Google:ClientId"];
        var redirectUri = _config["Google:RedirectUri"];
        var scope = "https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";

        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                    $"?client_id={clientId}" +
                    $"&redirect_uri={redirectUri}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString(scope)}" +
                    $"&access_type=offline" +
                    $"&prompt=consent";

        return Redirect(authUrl);
    }

    [HttpGet("youtube/callback")]
    public async Task<IActionResult> YouTubeCallback(string code, int userId)
    {
        try
        {
            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            using var httpClient = new HttpClient();

            // 1️⃣ Exchange authorization code for access token
            var tokenRequest = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest)
            );

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenJson = JsonDocument.Parse(tokenContent).RootElement;

            var accessToken = tokenJson.GetProperty("access_token").GetString();
            var refreshToken = tokenJson.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresIn = tokenJson.GetProperty("expires_in").GetInt32();
            var expiry = DateTime.UtcNow.AddSeconds(expiresIn);

            // 2️⃣ Fetch YouTube channel details
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var channelResponse = await httpClient.GetAsync("https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true");
            var channelJson = JsonDocument.Parse(await channelResponse.Content.ReadAsStringAsync());
            var channelItem = channelJson.RootElement.GetProperty("items")[0].GetProperty("snippet");

            var channelId = channelJson.RootElement.GetProperty("items")[0].GetProperty("id").GetString();
            var channelName = channelItem.GetProperty("title").GetString();
            var channelPicture = channelItem.GetProperty("thumbnails").GetProperty("default").GetProperty("url").GetString();

            // 3️⃣ Fetch user info from Google
            var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
            var userInfoJson = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync()).RootElement;
            var email = userInfoJson.GetProperty("email").GetString();

            // 4️⃣ Save to database
            var customer = await _customerRepository.Get(x => x.UserId == userId);
            if (customer == null)
                return BadRequest("Customer not found.");

            customer.YouTubeAccessToken = accessToken;
            customer.YouTubeRefreshToken = refreshToken;
            customer.YouTubeTokenExpiry = expiry;
            customer.YouTubeChannelId = channelId;
            customer.YouTubeChannelName = channelName;
            customer.YouTubeProfilePicture = channelPicture;
            customer.YouTubeEmail = email;

            await _customerRepository.Update(customer);

            return Redirect($"{_config["App:FrontendUrl"]}/dashboard?connected=youtube");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error connecting YouTube: {ex.Message}");
        }
    }

    [HttpGet("connect/tiktok")]
    public IActionResult ConnectTikTok()
    {
        var clientKey = _config["TikTok:ClientKey"];
        var redirectUri = $"{_config["App:BaseUrl"]}/api/socialauth/tiktok/callback";
        var scope = "user.info.basic,video.upload";
        var url = $"https://www.tiktok.com/v2/auth/authorize/?client_key={clientKey}&response_type=code&scope={scope}&redirect_uri={redirectUri}&state=secureRandomState";
        return Redirect(url);
    }

    [HttpGet("tiktok/callback")]
    public async Task<IActionResult> TikTokCallback(string code, int userId)
    {
        var tokenUrl = "https://open-api.tiktok.com/oauth/access_token/";
        var clientKey = _config["TikTok:ClientKey"];
        var clientSecret = _config["TikTok:ClientSecret"];
        var data = new Dictionary<string, string>
        {
            { "client_key", clientKey },
            { "client_secret", clientSecret },
            { "code", code },
            { "grant_type", "authorization_code" }
        };
        using var http = new HttpClient();
        var res = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(data));
        var json = await res.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(json).RootElement.GetProperty("data").GetProperty("access_token").GetString();
        var customer = await _customerRepository.Get(x => x.UserId == userId);
        customer.TikTokAccessToken = token;
        await _customerRepository.Update(customer);

        return Redirect($"{_config["App:FrontendUrl"]}/dashboard?connected=tiktok");
    }

}