using FullPost.Entities;
namespace FullPost.Interfaces.Rules;

public interface ISubscriptionPlanRules
{
    Task<List<string>> GetAllowedPlatformsForUser(int planId, int userId);
}