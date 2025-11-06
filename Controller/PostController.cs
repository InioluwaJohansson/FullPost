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
public class PostController : Controller
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
    {
        var result = await _postService.CreatePostAsync(request);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPut("edit/{postId}")]
    public async Task<IActionResult> EditPost([FromForm] EditPostDto request)
    {
        var result = await _postService.EditPostAsync(request);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("delete/{postId}")]
    public async Task<IActionResult> DeletePost(string postId, int userId)
    {
        var result = await _postService.DeletePostAsync(postId, userId);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpGet("all/{userId}")]
    public async Task<IActionResult> GetAllPosts(int userId)
    {
        var result = await _postService.GetAllPostsAsync(userId);
        return Ok(result);
    }
}