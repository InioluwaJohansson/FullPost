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
public class PostController : Controller
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != request.UserId)
            return Forbid("You are not allowed to access this user's data.");
        
        var result = await _postService.CreatePostAsync(request);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpPut("edit/{postId}")]
    public async Task<IActionResult> EditPost([FromForm] EditPostDto request)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != request.UserId)
            return Forbid("You are not allowed to access this user's data.");

        var result = await _postService.EditPostAsync(request);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpDelete("delete/{postId}")]
    public async Task<IActionResult> DeletePost(string postId, int userId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");

        var result = await _postService.DeletePostAsync(postId, userId);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    [Authorize]
    [HttpGet("allposts/{userId}")]
    public async Task<IActionResult> GetAllPosts(int userId)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        
        var result = await _postService.GetAllPostsAsync(userId);
        return Ok(result);
    }
}