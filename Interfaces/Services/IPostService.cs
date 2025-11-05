using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
using FullPost.Entities;
namespace FullPost.Interfaces.Services;

public interface IPostService
{
    Task<BaseResponse> CreatePostAsync(int customerId,string caption,List<IFormFile>? mediaFiles = null,List<string>? platforms = null);

    Task<BaseResponse> EditPostAsync(string postId, int customerId, string newCaption, List<IFormFile>? newMedia = null);

    Task<BaseResponse> DeletePostAsync(string postId, int customerId);

    Task<PostsResponseModel> GetAllPostsAsync(int customerId, int limit = 5);
}
