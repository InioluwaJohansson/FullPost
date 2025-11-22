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
    [HttpGet("allposts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetAllPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetAllPostsAsync(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetTwitterPosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetTwitterPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetTwitterPosts(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetFacebookPosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetFacebookPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetFacebookPosts(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetInstagramPosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetInstagramPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetInstagramPosts(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetYouTubePosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetYouTubePosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetYouTubePosts(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetTikTokPosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetTikTokPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetTikTokPosts(userId, start, limit);
        return Ok(result);
    }
    [Authorize]
    [HttpGet("GetLinkedInPosts/{userId}/{start}/{limit}")]
    public async Task<IActionResult> GetLinkedInPosts(int userId, int start, int limit)
    {
        var loggedInUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loggedInUserIdString, out int loggedInUserId))
            return Unauthorized();
        if (loggedInUserId != userId)
            return Forbid("You are not allowed to access this user's data.");
        var result = await _postService.GetLinkedInPosts(userId, start, limit);
        return Ok(result);
    }
}