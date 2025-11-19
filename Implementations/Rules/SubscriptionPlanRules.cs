using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Rules;
using FullPost.Models.Enums;

namespace FullPost.Implementations.Rules;
public class SubscriptionPlanRules : ISubscriptionPlanRules
{
    private readonly ISubscriptionPlanRepo _subscriptionPlanRepo;
    private readonly ICustomerRepo _customerRepo;

    SubscriptionPlanRules(ISubscriptionPlanRepo subscriptionPlanRepo, ICustomerRepo customerRepo)
    {
        _subscriptionPlanRepo = subscriptionPlanRepo;
        _customerRepo = customerRepo;
    }
    public static readonly Dictionary<SubscriptionPlans, int> PlatformLimit = new()
    {
        { SubscriptionPlans.Basic, 2 },
        { SubscriptionPlans.Standard, 5 },
        { SubscriptionPlans.Premium, int.MaxValue }
    };
    public static readonly List<string> BasicPlatforms = new()
    {
        "facebook",
        "instagram"
    };
    public async Task<List<string>> GetAllowedPlatformsForUser(int planId, int userId)
    {
        var connectedPlatforms = new List<string>();
        var user = await _customerRepo.Get(x => x.UserId == userId);
        var plan = await _subscriptionPlanRepo.Get(x => x.Id == planId);
        if (plan != null && user != null)
        {
            if(user.TwitterUsername != "") connectedPlatforms.Add("twitter");
            if(user.InstagramUsername != "") connectedPlatforms.Add("instagram");
            if(user.YouTubeChannelName != "") connectedPlatforms.Add("youtube");
            if(user.FacebookPageName != "") connectedPlatforms.Add("facebook");
            if(user.TikTokUsername != "") connectedPlatforms.Add("twitter");
            if(user.LinkedInUsername != "") connectedPlatforms.Add("linkedin");
            var limit = SubscriptionPlanRules.PlatformLimit[plan.PlanType];
            if (plan.PlanType == SubscriptionPlans.Basic) return connectedPlatforms.Where(p => SubscriptionPlanRules.BasicPlatforms.Contains(p)).ToList();
            if (plan.PlanType == SubscriptionPlans.Standard) return connectedPlatforms.Take(limit).ToList();
            return connectedPlatforms;
        }
        return new List<string>();
    }
}
