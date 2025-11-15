using FullPost.Entities;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface ISubscriptionService
{
    Task<(bool,string)> CheckUserSubscriptionStatus(int userId);
    Task AutoSubscribeSubscription();
    Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto);
    Task<BaseResponse> SubscribeUserAsync(int userId, int planId);
    Task<BaseResponse> CancelSubscriptionAsync(string subscriptionCode);
    Task<SubscriptionPlanResponseModel> GetAllPlansAsync();
    Task<UserSubscriptionResponseModel> GetUserSubscriptionsAsync(int userId);
}