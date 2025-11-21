using FullPost.Entities;
using FullPost.Models.DTOs;
namespace FullPost.Interfaces.Services;
public interface ISubscriptionService
{
    Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto);
    Task<BaseResponse> UpdateLocalPlanAsync(UpdateSubscriptionDto updateSubscriptionDto);
    Task<string?> GenerateSubscriptionPaymentLink(int userId, int planId);
    Task<BaseResponse> CancelUserSubscriptionAsync(int userId, int subId);
    Task<AutoSubscribeResponseModel> EnableCancelUserAutoSubscribe(int userId);
    Task<SubscriptionPlanResponseModel> GetAllPlansAsync();
    Task<AdminSubscriptionPlanResponseModel> GetAdminSubscriptionsAsync();
    Task<UserSubscriptionResponseModel> GetUserSubscriptionsAsync(int userId);
    Task OnSubscriptionRenewed(dynamic data);
    Task OnSubscriptionPaymentFailed(dynamic data);
    Task OnInitialSubscriptionPaid(dynamic data);
    Task CheckRenewals();
    Task ResetMonthlyPostCountAsync();
    Task ResetToBasic();
}