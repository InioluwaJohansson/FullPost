using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Interfaces.Services;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FullPost.Controllers;

[ApiController]
[Route("FullPost/[controller]")]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }
    [HttpPost("create")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionDto createSubscriptionDto)
    {        
        var result = await _subscriptionService.CreatePlanAsync(createSubscriptionDto);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();

        var plans = await _subscriptionService.GetAllPlansAsync();
        return Ok(plans);
    }
    [Authorize]
    [HttpPost("cancel/{userId}/{subId}")]
    public async Task<IActionResult> CancelUserSubscriptionAsync(int userId, int subId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");

        var result = await _subscriptionService.CancelUserSubscriptionAsync(userId, subId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpPost("generatePaymentLink/{userId}/{planId}")]
    public async Task<IActionResult> GenerateSubscriptionPaymentLink(int userId, int planId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");

        var result = await _subscriptionService.GenerateSubscriptionPaymentLink(userId, planId);
        return result != null ? Ok(result) : BadRequest("Failed to generate payment link.");
    }
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserSubscriptions(int userId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        
        var result = await _subscriptionService.GetUserSubscriptionsAsync(userId);
        return Ok(result);
    }
    [HttpGet("admin/")]
    public async Task<IActionResult> GetAdminSubscriptionsAsync()
    {
        var result = await _subscriptionService.GetAdminSubscriptionsAsync();
        return Ok(result);
    }
}