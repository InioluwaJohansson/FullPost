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
    Task<PostsResponseModel> GetAllPostsAsync(int userId, int start, int limit = 50);
    Task<TwitterResponseModel> GetTwitterPosts(int userId, long? sinceId = null, int limit = 100);
    Task<FacebookResponseModel> GetFacebookPosts(int userId, int start, int limit);
    Task<InstagramResponseModel> GetInstagramPosts(int userId, string limit, int start = 50);
    Task<YouTubeResponseModel> GetYouTubePosts(int userId, int start, int limit);
    Task<TikTokResponseModel> GetTikTokPosts(int userId, int start, int limit);
    Task<LinkedInResponseModel> GetLinkedInPosts(int userId, int start, int limit);
}
