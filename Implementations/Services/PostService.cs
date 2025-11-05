using System.Text.Json;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Tweetinvi.Models;

namespace FullPost.Implementations.Services;
public class PostService : IPostService
{
    private readonly ITwitterService _twitterService;
    private readonly IFacebookService _facebookService;
    private readonly IInstagramService _instagramService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPostRepo _repository;
    private readonly ICustomerRepo _customerRepository;

    public PostService(ITwitterService twitterService, IFacebookService facebookService, IInstagramService instagramService, ISubscriptionService subscriptionService, IPostRepo repository, ICustomerRepo customerRepository)
    {
        _twitterService = twitterService;
        _facebookService = facebookService;
        _instagramService = instagramService;
        _subscriptionService = subscriptionService;
        _repository = repository;
        _customerRepository = customerRepository;
    }

    public async Task<BaseResponse> CreatePostAsync(int userId,string caption, List<IFormFile>? mediaFiles = null,List<string>? platforms = null)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId);
        if (customer == null || await _subscriptionService.CheckUserSubscriptionStatus(userId) == false)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Customer not found. Cannot create post. Login required."
            };
        }
        try
        {
            platforms ??= new List<string> { "twitter", "facebook", "instagram" };
            string? facebookId = null, instagramId = null; ITweet twitterId = null;
            List<string> uploadedUrls = new();
            foreach (var platform in platforms)
            {
                switch (platform.ToLower())
                {
                    case "twitter":
                        var tResult = await _twitterService.PostTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, caption, mediaFiles);
                        twitterId = tResult;
                        break;

                    case "facebook":
                        var fResult = await _facebookService.CreatePostAsync(customer.FacebookPageId!, customer.FacebookAccessToken!, caption, mediaFiles);
                        facebookId = fResult;
                        break;

                    case "instagram":
                        var iResult = await _instagramService.CreatePostAsync(customer.InstagramUserId!, customer.InstagramAccessToken!, caption, mediaFiles);
                        instagramId = iResult;
                        break;
                }
            }

            var post = new Post
            {
                UserId = customer.UserId,
                Caption = caption,
                MediaUrls = JsonSerializer.Serialize(uploadedUrls),
                TwitterPostId = twitterId.IdStr,
                FacebookPostId = facebookId,
                InstagramPostId = instagramId
            };
            await _repository.Create(post);
            return new BaseResponse
            {
                Status = true,
                Message = "Post created successfully and saved to database."
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse
            {
                Status = false,
                Message = $"Failed to create post: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse> EditPostAsync(string postId, int userId, string newCaption, List<IFormFile>? newMedia = null)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId);
        if (customer == null)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Customer not found. Cannot create post. Login required."
            };
        }
        try
        {
            var post = await _repository.Get(x => x.PostId == postId);
            if (post == null || await _subscriptionService.CheckUserSubscriptionStatus(userId))
                return new BaseResponse { Status = false, Message = "Post not found. Or subscription Expired!" };

            if (!string.IsNullOrEmpty(post.TwitterPostId.ToString()))
                await _twitterService.EditTweetAsync(
                    customer.TwitterAccessToken!,
                    customer.TwitterAccessSecret!,
                    post.TwitterPostId,
                    newCaption,
                    newMedia
                );

            if (!string.IsNullOrEmpty(post.FacebookPostId))
                await _facebookService.EditPostAsync(
                    customer.FacebookPageId!,
                    customer.FacebookAccessToken!,
                    post.FacebookPostId,
                    newCaption,
                    newMedia
                );

            if (!string.IsNullOrEmpty(post.InstagramPostId))
                await _instagramService.EditPostAsync(
                    customer.InstagramUserId!,
                    customer.InstagramAccessToken!,
                    post.InstagramPostId,
                    newCaption,
                    newMedia
                );

            post.Caption = newCaption;
            post.LastModifiedOn = DateTime.UtcNow;
            await _repository.Update(post);

            return new BaseResponse
            {
                Status = true,
                Message = "Post edited successfully on all platforms and updated in database."
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse
            {
                Status = false,
                Message = $"Failed to edit post: {ex.Message}"
            };
        }
    }
    public async Task<BaseResponse> DeletePostAsync(string postId, int customerId)
    {
        var customer = await _customerRepository.Get(c => c.Id == customerId);
        if (customer == null)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Customer not found. Cannot delete post. Login required."
            };
        }
        try
        {
            var post = await _repository.Get(x => x.PostId == postId);
            if (post == null)
                return new BaseResponse { Status = false, Message = "Post not found." };

            if (!string.IsNullOrEmpty(post.TwitterPostId.ToString()))
                await _twitterService.DeleteTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, post.TwitterPostId);

            if (!string.IsNullOrEmpty(post.FacebookPostId))
                await _facebookService.DeletePostAsync(customer.FacebookAccessToken!, post.FacebookPostId);

            if (!string.IsNullOrEmpty(post.InstagramPostId))
                await _instagramService.DeletePostAsync(customer.InstagramAccessToken!, post.InstagramPostId);

            await _repository.Delete(post);

            return new BaseResponse
            {
                Status = true,
                Message = "Post deleted successfully from all platforms and database."
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse
            {
                Status = false,
                Message = $"Failed to delete post: {ex.Message}"
            };
        }
    }
    public async Task<PostsResponseModel> GetAllPostsAsync(int userId, int limit = 5)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId);
        if (customer == null)
        {
            return new PostsResponseModel
            {
                Status = false,
                Message = "Customer not found. Cannot create post. Login required."
            };
        }
        var allPosts = new List<GetPostDto>();
        try
        {
            var tweets = await _twitterService.GetUserTweetsAsync(customer.TwitterAccessToken, customer.TwitterAccessSecret, limit);
            allPosts.AddRange(tweets.Select(t => new GetPostDto
            {
                Platform = "Twitter",
                Id = t.IdStr,
                Text = t.FullText,
                CreatedAt = t.CreatedAt.UtcDateTime,
                MediaUrl = t.Media?.FirstOrDefault()?.MediaURLHttps,
                Permalink = $"https://twitter.com/{t.CreatedBy.ScreenName}/status/{t.IdStr}"
            }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Twitter Error]: {ex.Message}");
        }
        try
        {
            var fbResponse = await _facebookService.GetPostsAsync(customer.FacebookAccessToken, customer.FacebookPageId, limit);
            var fbJson = JsonDocument.Parse(fbResponse);

            if (fbJson.RootElement.TryGetProperty("data", out var fbPosts))
            {
                foreach (var post in fbPosts.EnumerateArray())
                {
                    allPosts.Add(new GetPostDto
                    {
                        Platform = "Facebook",
                        Id = post.GetProperty("id").GetString(),
                        Text = post.TryGetProperty("message", out var msg) ? msg.GetString() : "",
                        MediaUrl = post.TryGetProperty("full_picture", out var pic) ? pic.GetString() : null,
                        Permalink = post.TryGetProperty("permalink_url", out var link) ? link.GetString() : null,
                        CreatedAt = post.TryGetProperty("created_time", out var time) ? DateTime.Parse(time.GetString()) : DateTime.MinValue
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Facebook Error]: {ex.Message}");
        }
        try
        {
            var igResponse = await _instagramService.GetPostsAsync(customer.InstagramUserId, customer.InstagramAccessToken, limit);
            var igJson = JsonDocument.Parse(igResponse);

            if (igJson.RootElement.TryGetProperty("data", out var igPosts))
            {
                foreach (var post in igPosts.EnumerateArray())
                {
                    allPosts.Add(new GetPostDto
                    {
                        Platform = "Instagram",
                        Id = post.GetProperty("id").GetString(),
                        Text = post.TryGetProperty("caption", out var cap) ? cap.GetString() : "",
                        MediaUrl = post.TryGetProperty("media_url", out var url) ? url.GetString() : null,
                        Permalink = post.TryGetProperty("permalink", out var link) ? link.GetString() : null,
                        CreatedAt = post.TryGetProperty("timestamp", out var time) ? DateTime.Parse(time.GetString()): DateTime.MinValue
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Instagram Error]: {ex.Message}");
        }
        var homePosts = await _repository.GetByExpression(x => x.UserId == userId);
        if (homePosts != null)
        {
            foreach (var post in homePosts)
            {
                allPosts.Add(new GetPostDto
                {
                    Platform = "default",
                    Id = post.Id.ToString(),
                    Text = post.Caption,
                    MediaUrl = post.MediaUrls, // might need deserialization
                    Permalink = "",
                    CreatedAt = post.CreatedOn,
                });
            }
        }
        return new PostsResponseModel
        {
            Status = true,
            Data = allPosts.OrderByDescending(p => p.CreatedAt).ToList()
        };
    }
}
