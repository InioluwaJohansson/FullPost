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
    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionDto createSubscriptionDto)
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();
        
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
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(int userId, int planId)
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();

        var result = await _subscriptionService.SubscribeUserAsync(userId, planId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpPost("verifyAndActivateSubscriptionAsync")]
    public async Task<IActionResult> VerifyAndActivateSubscriptionAsync(string reference, int userId, int planId)
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();

        var result = await _subscriptionService.VerifyAndActivateSubscriptionAsync(reference, userId, planId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpPost("cancel/{userId}/{subId}")]
    public async Task<IActionResult> CancelUserSubscriptionAsync(int userId, int subId)
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();

        var result = await _subscriptionService.CancelUserSubscriptionAsync(userId, subId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserSubscriptions(int userId)
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user == null) return Unauthorized();
        
        var result = await _subscriptionService.GetUserSubscriptionsAsync(userId);
        return Ok(result);
    }
}