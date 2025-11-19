using FullPost.Entities;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface ISubscriptionService
{
    Task OnSubscriptionRenewed(dynamic data);
    Task OnSubscriptionPaymentFailed(dynamic data);
    Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto);
    Task<AdminSubscriptionPlanResponseModel> GetAdminSubscriptionsAsync();
    Task<string?> GenerateSubscriptionPaymentLink(int userId, int planId);
    Task<BaseResponse> SubscribeUserAsync(int userId, int planId);
    Task<BaseResponse> VerifyAndActivateSubscriptionAsync(string reference, int userId, int planId);
    Task<BaseResponse> CancelUserSubscriptionAsync(int userId, int subId);
    Task<SubscriptionPlanResponseModel> GetAllPlansAsync();
    Task<UserSubscriptionResponseModel> GetUserSubscriptionsAsync(int userId);
    Task ResetMonthlyPostCountAsync();
    Task ResetToBasic();
}