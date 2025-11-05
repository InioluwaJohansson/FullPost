using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FullPost.Interfaces.Services;
public interface IInstagramService
{
    Task<string> CreatePostAsync(string igUserId,string accessToken,string caption,List<IFormFile>? mediaFiles = null);

    Task<string> EditPostAsync(string igUserId,string accessToken,string oldMediaId,string newCaption,List<IFormFile>? newMedia = null);

    Task<bool> DeletePostAsync(string accessToken, string mediaId);

    Task<string> GetPostsAsync(string igUserId,string accessToken,int limit = 5);
}
