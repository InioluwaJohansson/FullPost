using System.Text.Json;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Tweetinvi.Models;

namespace FullPost.Implementations.Services
{
    public class PostService : IPostService
    {
        private readonly ITwitterService _twitterService;
        private readonly IFacebookService _facebookService;
        private readonly IInstagramService _instagramService;
        private readonly IYouTubeService _youtubeService;
        private readonly ITikTokService _tiktokService;
        private readonly ILinkedInService _linkedinService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPostRepo _repository;
        private readonly ICustomerRepo _customerRepository;
        private readonly IEmailService _emailService;

        public PostService(ITwitterService twitterService,IFacebookService facebookService,IInstagramService instagramService,IYouTubeService youtubeService,ITikTokService tiktokService,ILinkedInService linkedinService,ISubscriptionService subscriptionService,IPostRepo repository,ICustomerRepo customerRepository,IEmailService emailService)
        {
            _twitterService = twitterService;
            _facebookService = facebookService;
            _instagramService = instagramService;
            _youtubeService = youtubeService;
            _tiktokService = tiktokService;
            _linkedinService = linkedinService;
            _subscriptionService = subscriptionService;
            _repository = repository;
            _customerRepository = customerRepository;
            _emailService = emailService;
        }

        public async Task<BaseResponse> CreatePostAsync(CreatePostDto createPostDto)
        {
            var customer = await _customerRepository.Get(c => c.UserId == createPostDto.UserId);
            if (customer == null || !await _subscriptionService.CheckUserSubscriptionStatus(createPostDto.UserId))
                return new BaseResponse { Status = false, Message = "Customer not found or subscription inactive." };

            try
            {
                createPostDto.Platforms ??= new List<string> { "twitter", "facebook", "instagram" };
                string? facebookId = null, instagramId = null, youtubeId = null, tiktokId = null, linkedinId = null;
                ITweet? twitterPost = null;
                List<string> uploadedUrls = new();

                foreach (var platform in createPostDto.Platforms)
                {
                    switch (platform.ToLower())
                    {
                        case "twitter":
                            twitterPost = await _twitterService.PostTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, createPostDto.Caption, createPostDto.MediaFiles);
                            break;

                        case "facebook":
                            facebookId = await _facebookService.CreatePostAsync(customer.FacebookPageId!, customer.FacebookAccessToken!, createPostDto.Caption, createPostDto.MediaFiles);
                            break;

                        case "instagram":
                            instagramId = await _instagramService.CreatePostAsync(customer.InstagramUserId!, customer.InstagramAccessToken!, createPostDto.Caption, createPostDto.MediaFiles);
                            break;

                        case "youtube":
                            if (createPostDto.MediaFiles is not null && createPostDto.MediaFiles.Any())
                                youtubeId = await _youtubeService.CreatePostAsync(customer.YouTubeAccessToken!, createPostDto.MediaFiles.First(), createPostDto.Caption, createPostDto.Caption);
                            break;

                        case "tiktok":
                            if (createPostDto.MediaFiles is not null && createPostDto.MediaFiles.Any())
                                tiktokId = await _tiktokService.CreatePostAsync(customer.TikTokAccessToken!, createPostDto.MediaFiles.First(), createPostDto.Caption);
                            break;

                        case "linkedin":
                            linkedinId = (await _linkedinService.CreatePostAsync(customer.LinkedInAccessToken!, customer.LinkedInUserId!, createPostDto.Caption)).ToString();
                            break;
                    }
                }

                var post = new Post
                {
                    UserId = customer.UserId,
                    Caption = createPostDto.Caption,
                    MediaUrls = JsonSerializer.Serialize(uploadedUrls),
                    TwitterPostId = twitterPost?.IdStr,
                    FacebookPostId = facebookId,
                    InstagramPostId = instagramId,
                    YouTubePostId = youtubeId,
                    TikTokPostId = tiktokId,
                    LinkedInPostId = linkedinId
                };

                await _repository.Create(post);
                await _emailService.SendEmailAsync(customer.User!.Email, "New Post Created", "Your post was successfully created and shared.");

                return new BaseResponse { Status = true, Message = "Post created successfully." };
            }
            catch (Exception ex)
            {
                return new BaseResponse { Status = false, Message = $"Failed to create post: {ex.Message}" };
            }
        }

        public async Task<BaseResponse> EditPostAsync(EditPostDto editPostDto)
        {
            var customer = await _customerRepository.Get(c => c.UserId == editPostDto.UserId);
            if (customer == null)
                return new BaseResponse { Status = false, Message = "Customer not found." };

            var post = await _repository.Get(x => x.PostId == editPostDto.PostId);
            if (post == null)
                return new BaseResponse { Status = false, Message = "Post not found." };

            try
            {
                if (!string.IsNullOrEmpty(post.TwitterPostId))
                    await _twitterService.EditTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, post.TwitterPostId, editPostDto.NewCaption, editPostDto.NewMediaFiles);

                if (!string.IsNullOrEmpty(post.FacebookPostId))
                    await _facebookService.EditPostAsync(customer.FacebookPageId!, customer.FacebookAccessToken!, post.FacebookPostId, editPostDto.NewCaption, editPostDto.NewMediaFiles);

                if (!string.IsNullOrEmpty(post.InstagramPostId))
                    await _instagramService.EditPostAsync(customer.InstagramUserId!, customer.InstagramAccessToken!, post.InstagramPostId, editPostDto.NewCaption, editPostDto.NewMediaFiles);

                if (!string.IsNullOrEmpty(post.YouTubePostId))
                    await _youtubeService.EditPostAsync(customer.YouTubeAccessToken!, post.YouTubePostId, editPostDto.NewCaption, editPostDto.NewCaption);

                if (!string.IsNullOrEmpty(post.TikTokPostId))
                    await _tiktokService.EditPostAsync(customer.TikTokAccessToken!, post.TikTokPostId, editPostDto.NewCaption);

                if (!string.IsNullOrEmpty(post.LinkedInPostId))
                    await _linkedinService.EditPostAsync(customer.LinkedInAccessToken!, post.LinkedInPostId, editPostDto.NewCaption);

                post.Caption = editPostDto.NewCaption;
                post.LastModifiedOn = DateTime.UtcNow;
                await _repository.Update(post);

                await _emailService.SendEmailAsync(customer.User!.Email, "Post Updated", "Your post has been updated successfully.");
                return new BaseResponse { Status = true, Message = "Post updated successfully." };
            }
            catch (Exception ex)
            {
                return new BaseResponse { Status = false, Message = $"Failed to edit post: {ex.Message}" };
            }
        }

        public async Task<BaseResponse> DeletePostAsync(string postId, int customerId)
        {
            var customer = await _customerRepository.Get(c => c.Id == customerId);
            if (customer == null)
                return new BaseResponse { Status = false, Message = "Customer not found." };

            var post = await _repository.Get(x => x.PostId == postId);
            if (post == null)
                return new BaseResponse { Status = false, Message = "Post not found." };

            try
            {
                if (!string.IsNullOrEmpty(post.TwitterPostId))
                    await _twitterService.DeleteTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, post.TwitterPostId);

                if (!string.IsNullOrEmpty(post.FacebookPostId))
                    await _facebookService.DeletePostAsync(customer.FacebookAccessToken!, post.FacebookPostId);

                if (!string.IsNullOrEmpty(post.InstagramPostId))
                    await _instagramService.DeletePostAsync(customer.InstagramAccessToken!, post.InstagramPostId);

                if (!string.IsNullOrEmpty(post.YouTubePostId))
                    await _youtubeService.DeletePostAsync(customer.YouTubeAccessToken!, post.YouTubePostId);

                if (!string.IsNullOrEmpty(post.TikTokPostId))
                    await _tiktokService.DeletePostAsync(customer.TikTokAccessToken!, post.TikTokPostId);

                if (!string.IsNullOrEmpty(post.LinkedInPostId))
                    await _linkedinService.DeletePostAsync(customer.LinkedInAccessToken!, post.LinkedInPostId);

                await _repository.Delete(post);
                await _emailService.SendEmailAsync(customer.User!.Email, "Post Deleted", "Your post was deleted successfully.");

                return new BaseResponse { Status = true, Message = "Post deleted successfully." };
            }
            catch (Exception ex)
            {
                return new BaseResponse { Status = false, Message = $"Failed to delete post: {ex.Message}" };
            }
        }

        public async Task<PostsResponseModel> GetAllPostsAsync(int customerId, int limit)
        {
            var customer = await _customerRepository.Get(c => c.Id == customerId);
            if (customer == null)
                return new PostsResponseModel { Status = false, Message = "Customer not found." };

            try
            {
                var allPosts = new Dictionary<string, object>();

                var localPosts = await _repository.Get(p => p.UserId == customer.UserId);
                allPosts["local"] = localPosts;

                if (!string.IsNullOrEmpty(customer.TwitterAccessToken))
                    allPosts["twitter"] = await _twitterService.GetUserTweetsAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, limit);

                if (!string.IsNullOrEmpty(customer.FacebookAccessToken))
                    allPosts["facebook"] = await _facebookService.GetPostsAsync(customer.FacebookPageId!, customer.FacebookAccessToken!);

                if (!string.IsNullOrEmpty(customer.InstagramAccessToken))
                    allPosts["instagram"] = await _instagramService.GetPostsAsync(customer.InstagramUserId!, customer.InstagramAccessToken!);

                if (!string.IsNullOrEmpty(customer.YouTubeAccessToken))
                    allPosts["youtube"] = await _youtubeService.GetAllPostsAsync(customer.YouTubeAccessToken!, customer.YouTubeChannelId!);

                if (!string.IsNullOrEmpty(customer.TikTokAccessToken))
                    allPosts["tiktok"] = await _tiktokService.GetAllPostsAsync(customer.TikTokAccessToken!, customer.TikTokUserId!);

                if (!string.IsNullOrEmpty(customer.LinkedInAccessToken))
                    allPosts["linkedin"] = await _linkedinService.GetAllPostsAsync(customer.LinkedInAccessToken!, customer.LinkedInUserId!);

                return new PostsResponseModel
                {
                    Status = true,
                    Message = "All posts retrieved successfully.",
                    Data = allPosts
                };
            }
            catch (Exception ex)
            {
                return new PostsResponseModel { Status = false, Message = $"Failed to retrieve posts: {ex.Message}" };
            }
        }
    }
}
