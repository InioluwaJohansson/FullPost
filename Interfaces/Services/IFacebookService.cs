using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface IFacebookService
{
    Task<SocialPostResult> CreatePostAsync(string pageId,string accessToken,string message,List<IFormFile>? mediaFiles = null);

    Task<SocialPostResult> EditPostAsync(string pageId,string accessToken,string postId,string newMessage,List<IFormFile>? newMedia = null);

    Task<bool> DeletePostAsync(string accessToken,string postId);

    Task<string> GetPostsAsync(string pageId,string accessToken,int limit = 5);
}

