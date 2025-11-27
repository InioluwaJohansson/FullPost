using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;

namespace FullPost.Implementations.Services;
public class AnalyticsService
{
    private readonly ITwitterService _twitterService;
    private readonly IFacebookService _facebookService;
    private readonly IInstagramService _instagramService;
    private readonly IYouTubeService _youtubeService;
    private readonly ITikTokService _tiktokService;
    private readonly ILinkedInService _linkedinService;
    private readonly ICustomerRepo _customerRepo;
    private readonly IAnalyticsRepo _analyticsRepo;
    public AnalyticsService(ITwitterService twitterService, IFacebookService facebookService, IInstagramService instagramService, IYouTubeService youtubeService, ITikTokService tiktokService, ILinkedInService linkedinService, IAnalyticsRepo analyticsRepo, ICustomerRepo customerRepo)
    {
        _twitterService = twitterService;
        _facebookService = facebookService;
        _instagramService = instagramService;
        _youtubeService = youtubeService;
        _tiktokService = tiktokService;
        _linkedinService = linkedinService;
        _analyticsRepo = analyticsRepo;
        _customerRepo = customerRepo;
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
}