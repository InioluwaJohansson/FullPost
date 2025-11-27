using System.Collections.Generic;
using System.Threading.Tasks;
using FullPost.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace FullPost.Interfaces.Services;
public interface IInstagramService
{
    Task<SocialPostResult> CreatePostAsync(string igUserId,string accessToken,string caption,List<IFormFile>? mediaFiles = null);

    Task<SocialPostResult> EditPostAsync(string igUserId,string accessToken,string oldMediaId,string newCaption,List<IFormFile>? newMedia = null);

    Task<bool> DeletePostAsync(string accessToken, string mediaId);

    Task<(IList<InstagramPostResponse>, string)> GetPostsAsync(string igUserId, string accessToken, string limit, int start = 100);
    Task<PlatformStats> GetStats(string accessToken);
}
