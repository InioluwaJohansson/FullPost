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
        var planType = new SubscriptionPlans ();
        if (plan != null && user != null)
        {
            if(user.TwitterUsername != "") connectedPlatforms.Add("twitter");
            if(user.InstagramUsername != "") connectedPlatforms.Add("instagram");
            if(user.YouTubeChannelName != "") connectedPlatforms.Add("youtube");
            if(user.FacebookPageName != "") connectedPlatforms.Add("facebook");
            if(user.TikTokUsername != "") connectedPlatforms.Add("twitter");
            if(user.LinkedInUsername != "") connectedPlatforms.Add("linkedin");
            if(plan.Name == "Basic") planType = SubscriptionPlans.Basic;
            else if(plan.Name == "Standard") planType = SubscriptionPlans.Standard;
            else if(plan.Name == "Premium") planType = SubscriptionPlans.Premium;
            var limit = SubscriptionPlanRules.PlatformLimit[planType];

            if (plan.Name == "Basic") return connectedPlatforms.Where(p => SubscriptionPlanRules.BasicPlatforms.Contains(p)).ToList();
            if (plan.Name == "Standard") return connectedPlatforms.Take(limit).ToList();
            return connectedPlatforms;
        }
        return new List<string>();
    }
}
