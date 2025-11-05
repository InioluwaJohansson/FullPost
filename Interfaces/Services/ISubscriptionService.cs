using FullPost.Entities;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface ISubscriptionService
{
    Task<BaseResponse> CreatePlanAsync(string name, decimal amount, string interval, string description);
    Task<BaseResponse> SubscribeUserAsync(string userEmail, int planId);
    Task<BaseResponse> CancelSubscriptionAsync(string subscriptionCode);
    Task<IEnumerable<SubscriptionPlan>> GetAllPlansAsync();
    Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(string userId);
}