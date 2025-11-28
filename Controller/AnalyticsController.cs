using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Interfaces.Services;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FullPost.Controllers;
[ApiController]
[Route("FullPost/[controller]")]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }
    [Authorize]
    [HttpPost("GetUserAnalytics")]
    public async Task<IActionResult> GetUserAnalytics(int userId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _analyticsService.GetUserAnalytics(userId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
}
