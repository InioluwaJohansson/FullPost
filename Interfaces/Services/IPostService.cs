using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Entities;
namespace FullPost.Interfaces.Services;
public interface IPostService
{
    Task<BaseResponse> CreatePostAsync(CreatePostDto createPostDto);

    Task<BaseResponse> EditPostAsync(EditPostDto editPostDto);

    Task<BaseResponse> DeletePostAsync(string postId, int userId);

    Task<PostsResponseModel> GetAllPostsAsync(int userId, int limit = 5);
}
