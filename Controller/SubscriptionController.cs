using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Interfaces.Services;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

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

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAllPlansAsync();
        return Ok(plans);
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(int userId, int planId)
    {
        var result = await _subscriptionService.SubscribeUserAsync(userId, planId);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPost("cancel/{subscriptionCode}")]
    public async Task<IActionResult> CancelSubscription(string subscriptionCode)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionCode);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserSubscriptions(int userId)
    {
        var result = await _subscriptionService.GetUserSubscriptionsAsync(userId);
        return Ok(result);
    }
}