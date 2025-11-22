using System.Text.Json;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Rules;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using Tweetinvi.Models;

namespace FullPost.Implementations.Services;
public class PostService : IPostService
{
    private readonly ITwitterService _twitterService;
    private readonly IFacebookService _facebookService;
    private readonly IInstagramService _instagramService;
    private readonly IYouTubeService _youtubeService;
    private readonly ITikTokService _tiktokService;
    private readonly ILinkedInService _linkedinService;
    private readonly ISubscriptionPlanRules _subscriptionPlanRules;
    private readonly IPostRepo _repository;
    private readonly ICustomerRepo _customerRepository;
    private readonly IUserSubscriptionRepo _userSubscriptionRepo;
    private readonly IEmailService _emailService;

    public PostService(ITwitterService twitterService,IFacebookService facebookService,IInstagramService instagramService,IYouTubeService youtubeService,ITikTokService tiktokService,ILinkedInService linkedinService,ISubscriptionPlanRules subscriptionPlanRules,IPostRepo repository,ICustomerRepo customerRepository,IUserSubscriptionRepo userSubscriptionRepo,IEmailService emailService)
    {
        _twitterService = twitterService;
        _facebookService = facebookService;
        _instagramService = instagramService;
        _youtubeService = youtubeService;
        _tiktokService = tiktokService;
        _linkedinService = linkedinService;
        _subscriptionPlanRules = subscriptionPlanRules;
        _repository = repository;
        _customerRepository = customerRepository;
        _userSubscriptionRepo = userSubscriptionRepo;
        _emailService = emailService;
    }
    public async Task<BaseResponse> CreatePostAsync(CreatePostDto createPostDto)
    {
        var customer = await _customerRepository.Get(c => c.UserId == createPostDto.UserId && c.IsDeleted == false);
        var plan = (await _userSubscriptionRepo.GetUserSubscriptionsAsync(createPostDto.UserId)).LastOrDefault();

        if (createPostDto != null && customer == null && plan == null && !createPostDto.Platforms.Any() || plan.IsActive == false)
            return new BaseResponse { Status = false, Message = "Customer not found or subscription inactive." };

        if (plan.IsActive == true && plan != null)
        {
            if (plan.NoOfPostsThisMonth == plan.Plan.NoOfPosts && plan.Plan.Name == "Basic")
                return new BaseResponse { Status = false, Message = "You have reached the monthly limit for Basic plan." };

            if (plan.NoOfPostsThisMonth == plan.Plan.NoOfPosts && plan.Plan.Name == "Standard")
                return new BaseResponse { Status = false, Message = "You have reached the monthly limit for Standard plan." };

            if (plan.NoOfPostsThisMonth < plan.Plan.NoOfPosts && plan.Plan.Name == "Basic" || plan.NoOfPostsThisMonth < plan.Plan.NoOfPosts && plan.Plan.Name == "Standard" || plan.Plan.Name == "Premium")
            {
                var allowedPlatforms = await _subscriptionPlanRules.GetAllowedPlatformsForUser(plan.Plan.Id, createPostDto.UserId);
                if (allowedPlatforms == null || !allowedPlatforms.Any())
                    return new BaseResponse (){Status = false, Message = "User has no allowed platforms."};
                var validPlatforms = createPostDto.Platforms.Where(p => allowedPlatforms.Contains(p.ToLower())).ToList();
                try
                {
                    List<string> platForms = new();
                    
                    List<string> uploadedUrls = new();

                    SocialPostResult? resultT = null, resultF = null, resultY = null, resultI =null, resultLi = null, resultTik = null;
                    foreach (var platform in validPlatforms)
                    {
                        switch (platform.ToLower())
                        {
                            case "twitter":
                                resultT = await _twitterService.PostTweetAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!,createPostDto.Caption, createPostDto.MediaFiles);
                                break;

                            case "facebook":
                                resultF = await _facebookService.CreatePostAsync(customer.FacebookPageId!, customer.FacebookAccessToken!,createPostDto.Caption, createPostDto.MediaFiles);
                                break;

                            case "instagram":
                                resultI = await _instagramService.CreatePostAsync(customer.InstagramUserId!, customer.InstagramAccessToken!,createPostDto.Caption, createPostDto.MediaFiles);
                                break;

                            case "youtube":
                                if (createPostDto.MediaFiles?.Any() == true)
                                {
                                    resultY = await _youtubeService.CreatePostAsync(customer.YouTubeAccessToken!,createPostDto.MediaFiles.First(),createPostDto.Title,createPostDto.Caption);
                                }
                                break;

                            case "tiktok":
                                if (createPostDto.MediaFiles?.Any() == true)
                                {
                                    resultTik = await _tiktokService.CreatePostAsync(customer.TikTokAccessToken!,createPostDto.MediaFiles.First(),createPostDto.Caption);
                                }
                                break;

                            case "linkedin":
                                resultLi = await _linkedinService.CreatePostAsync(customer.LinkedInAccessToken!,customer.LinkedInUserId!,createPostDto.Caption, resultI.MediaUrls.FirstOrDefault());
                                break;
                        }
                        platForms.Add(platform.ToLower());

                        if (resultI?.MediaUrls != null)
                        {
                            uploadedUrls.AddRange(resultI.MediaUrls);
                            uploadedUrls.AddRange(resultY.MediaUrls);
                        }
                    }

                    var post = new Post
                    {
                        UserId = customer.UserId,
                        Title = createPostDto.Title,
                        Caption = createPostDto.Caption,
                        MediaUrls = JsonSerializer.Serialize(uploadedUrls),
                        Platform = JsonSerializer.Serialize(platForms),
                        TwitterPostId = resultT.PostId,
                        FacebookPostId = resultF.PostId,
                        InstagramPostId = resultI.PostId,
                        YouTubePostId = resultY.PostId,
                        TikTokPostId = resultTik.PostId,
                        LinkedInPostId = resultLi.PostId,

                        TwitterPostLink = resultT.Permalink,
                        FacebookPostLink = resultF.Permalink,
                        InstagramPostLink = resultI.Permalink,
                        YouTubePostLink = resultY.Permalink,
                        TikTokPostLink = resultTik.Permalink,
                        LinkedInPostLink = resultLi.Permalink,
                        IsDeleted = false
                    };

                    await _repository.Create(post);

                    plan.NoOfPostsThisMonth++;
                    await _userSubscriptionRepo.Update(plan);

                    await _emailService.SendEmailAsync(customer.User!.Email,
                        "New Post Created", "Your post was successfully created and shared.");

                    return new BaseResponse { Status = true, Message = "Post created successfully." };
                }
                catch (Exception ex)
                {
                    return new BaseResponse { Status = false, Message = $"Failed to create post: {ex.Message}" };
                }
            }
        }
        return new BaseResponse { Status = false, Message = "Customer not found or subscription inactive." };
    }
    public async Task<BaseResponse> EditPostAsync(EditPostDto editPostDto)
    {
        var customer = await _customerRepository.Get(c => c.UserId == editPostDto.UserId && c.IsDeleted == false);
        if (customer == null)
            return new BaseResponse { Status = false, Message = "Customer not found." };

        var post = await _repository.Get(x => x.PostId == editPostDto.PostId);
        if (post == null)
            return new BaseResponse { Status = false, Message = "Post not found." };

        SocialPostResult twitterResult = new SocialPostResult (), facebookResult = null, instagramResult = null, youtubeResult = null, tiktokResult = null, linkedinResult = null; 
        try
        {
            List<string> updatedMediaUrls = new();
            if (!string.IsNullOrEmpty(post.TwitterPostId))
            {
                twitterResult = await _twitterService.EditTweetAsync(
                    customer.TwitterAccessToken!,
                    customer.TwitterAccessSecret!,
                    post.TwitterPostId,
                    editPostDto.NewCaption,
                    editPostDto.NewMediaFiles
                );
                if (twitterResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(twitterResult.MediaUrls);
            }

            if (!string.IsNullOrEmpty(post.FacebookPostId))
            {
                facebookResult = await _facebookService.EditPostAsync(
                    customer.FacebookPageId!,
                    customer.FacebookAccessToken!,
                    post.FacebookPostId,
                    editPostDto.NewCaption,
                    editPostDto.NewMediaFiles
                );
                if (facebookResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(facebookResult.MediaUrls);
            }

            if (!string.IsNullOrEmpty(post.InstagramPostId))
            {
                instagramResult = await _instagramService.EditPostAsync(
                    customer.InstagramUserId!,
                    customer.InstagramAccessToken!,
                    post.InstagramPostId,
                    editPostDto.NewCaption,
                    editPostDto.NewMediaFiles
                );
                if (instagramResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(instagramResult.MediaUrls);
            }

            if (!string.IsNullOrEmpty(post.YouTubePostId) && editPostDto.NewMediaFiles?.Any() == true)
            {
                youtubeResult = await _youtubeService.EditPostAsync(
                    customer.YouTubeAccessToken!,
                    post.YouTubePostId,
                    editPostDto.NewTitle,
                    editPostDto.NewCaption
                );
                if (youtubeResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(youtubeResult.MediaUrls);
            }

            if (!string.IsNullOrEmpty(post.TikTokPostId))
            {
                tiktokResult = await _tiktokService.EditPostAsync(
                    customer.TikTokAccessToken!,
                    post.TikTokPostId,
                    editPostDto.NewCaption
                );

                if (tiktokResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(tiktokResult.MediaUrls);
            }

            if (!string.IsNullOrEmpty(post.LinkedInPostId))
            {
                linkedinResult = await _linkedinService.EditPostAsync(
                    customer.LinkedInAccessToken!,
                    post.LinkedInPostId,customer.LinkedInUserId,
                    editPostDto.NewCaption,
                    instagramResult.MediaUrls.FirstOrDefault()
                );

                if (linkedinResult.MediaUrls != null)
                    updatedMediaUrls.AddRange(linkedinResult.MediaUrls);
                post.LinkedInPostId = linkedinResult.PostId; // update with new post ID after delete/create
            }

            post.Title = editPostDto.NewTitle;
            post.Caption = editPostDto.NewCaption;
            post.MediaUrls = updatedMediaUrls.Any() ? JsonSerializer.Serialize(updatedMediaUrls) : null;
            post.TwitterPostId = twitterResult.PostId; // no change in ID
            post.FacebookPostId = facebookResult.PostId;
            post.InstagramPostId = instagramResult.PostId;
            post.YouTubePostId = youtubeResult.PostId;
            post.TikTokPostId = linkedinResult.PostId;
            post.TwitterPostLink = twitterResult.Permalink;
            post.FacebookPostLink = facebookResult.Permalink;
            post.InstagramPostLink = instagramResult.Permalink;
            post.YouTubePostLink = $"https://www.youtube.com/watch?v={youtubeResult.PostId}";
            post.TikTokPostLink = tiktokResult.Permalink;
            post.LinkedInPostLink = linkedinResult.Permalink;

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
    public async Task<BaseResponse> DeletePostAsync(string postId, int userId)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
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
            post.IsDeleted = true;
            await _repository.Update(post);
            await _emailService.SendEmailAsync(customer.User!.Email, "Post Deleted", "Your post was deleted successfully.");

            return new BaseResponse { Status = true, Message = "Post deleted successfully." };
        }
        catch (Exception ex)
        {
            return new BaseResponse { Status = false, Message = $"Failed to delete post: {ex.Message}" };
        }
    }
    public async Task<PostsResponseModel> GetAllPostsAsync(int userId, int start, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new PostsResponseModel { Status = false, Message = "Customer not found." };
        try
        {
            var posts = await _repository.GetByExpression(p => p.UserId == customer.UserId && p.IsDeleted == false);
            posts.OrderByDescending(p => p.CreatedOn).Skip(start).Take(limit).ToList();
            if(posts != null)
            {
                return new PostsResponseModel()
                {
                    Data = posts.Select(x => new GetPostDto()
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Caption = x.Caption,
                        CreatedAt = x.CreatedOn,
                        TwitterPostLink = x.TwitterPostLink,
                        FacebookPostLink = x.FacebookPostLink,
                        InstagramPostLink = x.InstagramPostLink,
                        YouTubePostLink = x.YouTubePostLink,
                        TikTokPostLink = x.TikTokPostLink,
                        LinkedInPostLink = x.LinkedInPostLink,
                        Platform = JsonSerializer.Deserialize<List<string>>(x.Platform),
                        MediaUrl = JsonSerializer.Deserialize<List<string>>(x.MediaUrls),
                    }).ToList(),
                    Status = true,
                    Message = "All posts retrieved successfully.",
                };
            }
            return new PostsResponseModel
            {
                Status = false,
                Message = "You've not made any posts yet!",
                Data = null
            };
        }
        catch (Exception ex)
        {
            return new PostsResponseModel { Status = false, Message = $"Failed to retrieve posts: {ex.Message}" };
        }
    }
    public async Task<TwitterResponseModel> GetTwitterPosts(int userId,int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new TwitterResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.TwitterAccessToken))
        {
            var twitterPosts = await _twitterService.GetUserTweetsAsync(customer.TwitterAccessToken!, customer.TwitterAccessSecret!, limit);
            if(twitterPosts != null)
                return new TwitterResponseModel { Data = twitterPosts, Status = true, Message = "Tweets retrieved" };
            return new TwitterResponseModel { Data = null, Status = false, Message = "Tweets unretrieved" };
        }
        return new TwitterResponseModel { Status = false, Message = "Error not logged in to twitter" };
    }
    public async Task<FacebookResponseModel> GetFacebookPosts(int userId, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new FacebookResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.FacebookAccessToken))
        {
                var facebookPosts = await _facebookService.GetPostsAsync(customer.FacebookPageId!, customer.FacebookAccessToken!, limit);
            if(facebookPosts != null)
                return new FacebookResponseModel { Data = facebookPosts, Status = true, Message = "Facebook posts retrieved" };
            return new FacebookResponseModel { Data = null, Status = false, Message = "Facebook posts unretrieved" };
        }
        return new FacebookResponseModel { Status = false, Message = "Error not logged in to facebook" };
    }
    public async Task<InstagramResponseModel> GetInstagramPosts(int userId, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new InstagramResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.InstagramAccessToken))
        {
            var instagramPosts = await _instagramService.GetPostsAsync(customer.InstagramUserId!, customer.InstagramAccessToken!, limit);
            if(instagramPosts != null)
                return new InstagramResponseModel { Data = instagramPosts, Status = true, Message = "Instagram posts retrieved" };
            return new InstagramResponseModel { Data = null, Status = false, Message = "Instagram posts unretrieved" };
        }
        return new InstagramResponseModel { Status = false, Message = "Error not logged in to instagram" };
    }
    public async Task<YouTubeResponseModel> GetYouTubePosts(int userId, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new YouTubeResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.YouTubeAccessToken!))
        {
            var youTubePosts = await _youtubeService.GetAllPostsAsync(customer.YouTubeAccessToken!, customer.YouTubeChannelId!, limit);
            if(youTubePosts != null)
                return new YouTubeResponseModel { Data = youTubePosts, Status = true, Message = "YouTube posts retrieved" };
            return new YouTubeResponseModel { Data = null, Status = false, Message = "YouTube posts unretrieved" };
        }
        return new YouTubeResponseModel { Status = false, Message = "Error not logged in to YouTube" };
    }
    public async Task<TikTokResponseModel> GetTikTokPosts(int userId, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new TikTokResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.TikTokAccessToken))
        {
            var tikTokPosts = await _tiktokService.GetAllPostsAsync(customer.TikTokAccessToken!, customer.TikTokUserId!, limit);
            if(tikTokPosts != null)
                return new TikTokResponseModel { Data = tikTokPosts, Status = true, Message = "TikTik posts retrieved" };
            return new TikTokResponseModel { Data = null, Status = false, Message = "TikTok posts unretrieved" };
        }
        return new TikTokResponseModel { Status = false, Message = "Error not logged in to TikTok" };
    }
    public async Task<LinkedInResponseModel> GetLinkedInPosts(int userId, int start, int limit)
    {
        var customer = await _customerRepository.Get(c => c.UserId == userId && c.IsDeleted == false);
        if (customer == null)
            return new LinkedInResponseModel { Status = false, Message = "Customer not found." };
        if (!string.IsNullOrEmpty(customer.LinkedInAccessToken))
        {
            var linkedInPosts = await _linkedinService.GetAllPostsAsync(customer.LinkedInAccessToken!, customer.LinkedInUserId!, start, limit);
            if(linkedInPosts != null)
                return new LinkedInResponseModel { Data = linkedInPosts, Status = true, Message = "TikTik posts retrieved" };
            return new LinkedInResponseModel { Data = null, Status = false, Message = "TikTok posts unretrieved" };
        }
        return new LinkedInResponseModel { Status = false, Message = "Error not logged in to TikTok" };
    }
}