using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using FullPost.Models.Enums;
namespace FullPost.Implementations.Services;
public class AnalyticsService : IAnalyticsService
{
    private readonly ITwitterService _twitterService;
    private readonly IFacebookService _facebookService;
    private readonly IInstagramService _instagramService;
    private readonly IYouTubeService _youtubeService;
    private readonly ITikTokService _tiktokService;
    private readonly ILinkedInService _linkedinService;
    private readonly ICustomerRepo _customerRepo;
    private readonly IAnalyticsRepo _analyticsRepo;
    private readonly IPostRepo _postRepo;
    public AnalyticsService(ITwitterService twitterService, IFacebookService facebookService, IInstagramService instagramService, IYouTubeService youtubeService, ITikTokService tiktokService, ILinkedInService linkedinService, IAnalyticsRepo analyticsRepo, ICustomerRepo customerRepo, IPostRepo postRepo)
    {
        _twitterService = twitterService;
        _facebookService = facebookService;
        _instagramService = instagramService;
        _youtubeService = youtubeService;
        _tiktokService = tiktokService;
        _linkedinService = linkedinService;
        _analyticsRepo = analyticsRepo;
        _customerRepo = customerRepo;
        _postRepo = postRepo;
    }
    public async Task GetSubscribersAnalyticsData()
    {
        var customers = await _customerRepo.GetAll();
        if (customers != null)
        {
            foreach (var customer in customers)
            {
                var analytics = new Analytic();
                analytics.UserId = customer.UserId;
                if (!string.IsNullOrEmpty(customer.TwitterUsername))
                {
                    var twitter = await _twitterService.GetStats(customer.TwitterAccessToken, customer.TwitterAccessSecret, null);
                    analytics.TwitterFollowers = twitter.Followers;
                    analytics.TwitterLikes = twitter.Likes;
                    analytics.TwitterViews = twitter.Views;
                }
                if (!string.IsNullOrEmpty(customer.FacebookPageName))
                {
                    var facebook = await _facebookService.GetStats(customer.FacebookAccessToken);
                    analytics.FacebookFollowers = facebook.Followers;
                    analytics.FacebookReactions = facebook.Likes;
                    analytics.FacebookViews = facebook.Views;
                }
                if (!string.IsNullOrEmpty(customer.InstagramUsername))
                {
                    var instagram = await _instagramService.GetStats(customer.InstagramAccessToken);
                    analytics.InstagramFollowers = instagram.Followers;
                    analytics.InstagramLikes = instagram.Likes;
                    analytics.InstagramViews = instagram.Views;
                }
                if (!string.IsNullOrEmpty(customer.YouTubeChannelName))
                {
                    var youTube = await _youtubeService.GetStats(customer.YouTubeAccessToken);
                    analytics.YouTubeSubscribers = youTube.Followers;
                    analytics.YouTubeLikes = youTube.Likes;
                    analytics.YouTubeViews = youTube.Views;
                }
                if (!string.IsNullOrEmpty(customer.TikTokUsername))
                {
                    var tikTok = await _tiktokService.GetStats(customer.TikTokAccessToken, customer.TikTokUserId);
                    analytics.TikTokFollowers = tikTok.Followers;
                    analytics.TikTokLikes = tikTok.Likes;
                    analytics.TikTokViews = tikTok.Views;
                }
                if (!string.IsNullOrEmpty(customer.LinkedInUsername))
                {
                    var linkedIn = await _linkedinService.GetStats(customer.LinkedInAccessToken, customer.LinkedInUserId);
                    analytics.LinkedInConnections = linkedIn.Followers;
                    analytics.LinkedInLikes = linkedIn.Likes;
                    analytics.LinkedInViews = linkedIn.Views;
                }
                analytics.CreatedOn = DateTime.Now;
                analytics.IsDeleted = false;
                await _analyticsRepo.Create(analytics);
            }
        }
    }
    public async Task<AnalyticsResponseModel> GetUserAnalytics(int userId)
    {
        var user = await _customerRepo.GetById(userId);
        if(user != null)
        {
            var analytics = await _analyticsRepo.GetByExpression(x => x.UserId == userId && x.IsDeleted == false);
            var analyticsList = new List<GetAnalyticsDto>();
            if(analytics != null && user.User.SubscriptionPlan == SubscriptionPlans.Premium)
            {
                analyticsList = analytics.Select(x => new GetAnalyticsDto(){
                        UserId = x.UserId,
                        TwitterFollowers = x.TwitterFollowers,
                        TwitterLikes = x.TwitterLikes,
                        TwitterViews = x.TwitterViews,
                        FacebookFollowers = x.FacebookFollowers,
                        FacebookReactions = x.FacebookReactions,
                        FacebookViews = x.FacebookViews,
                        InstagramFollowers = x.InstagramFollowers,
                        InstagramLikes = x.InstagramLikes,
                        InstagramViews = x.InstagramViews,
                        YouTubeSubscribers = x.YouTubeSubscribers,
                        YouTubeViews = x.YouTubeViews,
                        YouTubeLikes = x.YouTubeLikes,
                        TikTokFollowers = x.TikTokFollowers,
                        TikTokViews = x.TikTokViews,
                        TikTokLikes = x.TikTokLikes,
                        LinkedInConnections = x.LinkedInConnections,
                        LinkedInLikes = x.LinkedInLikes,
                        LinkedInViews = x.LinkedInViews,
                        DateCreated = x.CreatedOn,
                    }).ToList();
                return new AnalyticsResponseModel()
                {
                    Data = analyticsList,
                    NoOfPosts = (await _postRepo.GetByExpression(x => x.UserId == userId && x.IsDeleted == false)).Count(),
                    TotalLikes = analyticsList.LastOrDefault().TwitterLikes + analyticsList.LastOrDefault().FacebookReactions + analyticsList.LastOrDefault().InstagramLikes + (int)analyticsList.LastOrDefault().YouTubeLikes + analyticsList.LastOrDefault().TikTokLikes + analyticsList.LastOrDefault().LinkedInLikes,
                    TotalReach = analyticsList.LastOrDefault().TwitterFollowers + analyticsList.LastOrDefault().FacebookFollowers + analyticsList.LastOrDefault().InstagramFollowers + (int)analyticsList.LastOrDefault().YouTubeSubscribers + analyticsList.LastOrDefault().TikTokFollowers + analyticsList.LastOrDefault().LinkedInConnections,
                    TotalViews = analyticsList.LastOrDefault().TwitterViews + analyticsList.LastOrDefault().FacebookViews + analyticsList.LastOrDefault().InstagramViews + (int)analyticsList.LastOrDefault().YouTubeViews + analyticsList.LastOrDefault().TikTokViews + analyticsList.LastOrDefault().LinkedInViews,
                    Status = true,
                    Message = "User Analytics retrieved"
                };
            }
            return new AnalyticsResponseModel()
            {
                Status = false,
                Message = "Failed to retrieve user analytics!",
                Data = null
            }; 
        }
        return new AnalyticsResponseModel()
        {
            Status = false,
            Message = "Upgrade to Premium to view analytics!",
            Data = null
        };
    }
}