using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FullPost.Interfaces.Services;

namespace FullPost.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
            return BadRequest("Missing Google ID token.");

        var customer = await _authService.GoogleLoginAsync(request.IdToken);
        return Ok(new { message = "Login successful", customer });
    }
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}
