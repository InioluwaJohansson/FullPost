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
    private readonly IYouTubeService _youtubeService;
    private readonly ITikTokService _tiktokService;
    private readonly ILinkedInService _linkedinService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPostRepo _repository;
    private readonly ICustomerRepo _customerRepository;
    private readonly ISubscriptionPlanRepo _subscriptionPlanRepo;
    private readonly IUserSubscriptionRepo _userSubscriptionRepo;
    private readonly IEmailService _emailService;

    public PostService(ITwitterService twitterService,IFacebookService facebookService,IInstagramService instagramService,IYouTubeService youtubeService,ITikTokService tiktokService,ILinkedInService linkedinService,ISubscriptionService subscriptionService,IPostRepo repository,ICustomerRepo customerRepository,ISubscriptionPlanRepo subscriptionPlanRepo,IUserSubscriptionRepo userSubscriptionRepo,IEmailService emailService)
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
        _subscriptionPlanRepo = subscriptionPlanRepo;
        _userSubscriptionRepo = userSubscriptionRepo;
        _emailService = emailService;
    }

    public async Task<BaseResponse> CreatePostAsync(CreatePostDto createPostDto)
    {
        var customer = await _customerRepository.Get(c => c.UserId == createPostDto.UserId);
        var subStatus = await _subscriptionService.CheckUserSubscriptionStatus(createPostDto.UserId);
        var plan = (await _userSubscriptionRepo.GetByExpression(x => x.UserId == createPostDto.UserId)).LastOrDefault();

        if (customer == null || subStatus.Item1 == false)
            return new BaseResponse { Status = false, Message = "Customer not found or subscription inactive." };

        if (subStatus.Item1 && plan != null)
        {
            if (plan.NoOfPostsThisMonth == 5 && subStatus.Item2 == "Free")
                return new BaseResponse { Status = false, Message = "You have reached the monthly limit for Free plan." };

            if (plan.NoOfPostsThisMonth == 15 && subStatus.Item2 == "Standard")
                return new BaseResponse { Status = false, Message = "You have reached the monthly limit for Standard plan." };

            if (plan.NoOfPostsThisMonth < 5 && subStatus.Item2 == "Free" ||
                plan.NoOfPostsThisMonth < 15 && subStatus.Item2 == "Standard" ||
                subStatus.Item2 == "Premium")
            {
                try
                {
                    createPostDto.Platforms ??= new List<string> { "twitter", "facebook", "instagram" };
                    List<string> uploadedUrls = new();

                    SocialPostResult? resultT = null, resultF = null, resultY = null, resultI =null, resultLi = null, resultTik = null;
                    foreach (var platform in createPostDto.Platforms)
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
        var customer = await _customerRepository.Get(c => c.UserId == editPostDto.UserId);
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