using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Interfaces.Services;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

namespace FullPost.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ICustomerRepository _customerRepository; // Optional, if you want to get user from DB

        public PostsController(IPostService postService, ICustomerRepository customerRepository)
        {
            _postService = postService;
            _customerRepository = customerRepository;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BaseResponse>> CreatePost([FromForm] string customerId,[FromForm] string caption,[FromForm] List<IFormFile>? mediaFiles,[FromForm] List<string>? platforms)
        {
            var customer = await _customerRepository.GetByIdAsync(Guid.Parse(customerId));
            if (customer == null)
                return NotFound(new BaseResponse { Status = false, Message = "Customer not found." });

            var result = await _postService.CreatePostAsync(customer, caption, mediaFiles, platforms);
            return Ok(result);
        }

        [HttpPut("edit/{postId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BaseResponse>> EditPost(string postId,[FromForm] string customerId,[FromForm] string newCaption,[FromForm] List<IFormFile>? newMedia)
        {
            var customer = await _customerRepository.GetByIdAsync(Guid.Parse(customerId));
            if (customer == null)
                return NotFound(new BaseResponse { Status = false, Message = "Customer not found." });

            var result = await _postService.EditPostAsync(postId, customer, newCaption, newMedia);
            return Ok(result);
        }

        [HttpDelete("delete/{postId}")]
        public async Task<ActionResult<BaseResponse>> DeletePost(Guid postId, [FromQuery] string customerId)
        {
            var customer = await _customerRepository.GetByIdAsync(Guid.Parse(customerId));
            if (customer == null)
                return NotFound(new BaseResponse { Status = false, Message = "Customer not found." });

            var result = await _postService.DeletePostAsync(postId, customer);
            return Ok(result);
        }
    }
}
