using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Entities;
using FullPost.Implementations.Respositories;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
namespace FullPost.Implementations.Services;
public class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _paystackSecretKey;
    private readonly ISubscriptionPlanRepo _subscriptionPlanRepo;
    private readonly IUserSubscriptionRepo _userSubscriptionRepo;
    private readonly IUserRepo _userRepo;
    private readonly IEmailService _emailService;

    public SubscriptionService(ISubscriptionPlanRepo subscriptionPlanRepo, IUserSubscriptionRepo userSubscriptionRepo, IConfiguration config, IUserRepo userRepo, IEmailService emailService)
    {
        _subscriptionPlanRepo = subscriptionPlanRepo;
        _userSubscriptionRepo = userSubscriptionRepo;
        _userRepo = userRepo;
        _httpClient = new HttpClient();
        _emailService = emailService;
        _paystackSecretKey = config["Paystack:SecretKey"]!;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSecretKey);
    }
    public async Task<(bool, string)> CheckUserSubscriptionStatus(int userId)
    {
        var userSubscription = await _userSubscriptionRepo.GetByExpression(x => x.UserId == userId);
        var subscription = await _subscriptionPlanRepo.Get(x => x.Id == userSubscription.LastOrDefault().PlanId);
        if (userSubscription != null && subscription != null && userSubscription.LastOrDefault().EndDate > DateTime.UtcNow) return (true,subscription.NoOfPosts);
        else return (false, null);
    }
    public async Task<BaseResponse> CancelSubscriptionAsync(string subscriptionCode)
    {
        var response = await _httpClient.PostAsync(
            $"https://api.paystack.co/subscription/disable",
            new StringContent(JsonSerializer.Serialize(new { code = subscriptionCode }),
            Encoding.UTF8, "application/json")
        );

        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Failed to cancel subscription: {result}" };

        var sub = await _userSubscriptionRepo.Get(s => s.PaystackSubscriptionCode == subscriptionCode);
        if (sub != null)
        {
            sub.IsActive = false;
            sub.EndDate = DateTime.UtcNow;
            await _userSubscriptionRepo.Update(sub);
        }
        var user = await _userRepo.Get(u => u.Id == sub.UserId);
        await _emailService.SendEmailAsync(user.Email, "Subscription Cancelled", "Your subscription has been successfully cancelled.");
        return new BaseResponse()
        {
            Status = true,
            Message = "Subscription cancelled successfully."
        };
    }

    public async Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto)
    {
        var payload = new
        {
            name = subscriptionDto.Name,
            amount = (int)(subscriptionDto.Amount * 100), // Paystack expects amount in kobo
            interval = subscriptionDto.Interval.ToString(),
            description = subscriptionDto.Description
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.paystack.co/plan", content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Failed to create plan: {result}" };

        var json = JsonDocument.Parse(result);
        var planCode = json.RootElement.GetProperty("data").GetProperty("plan_code").GetString();

        var plan = new SubscriptionPlan
        {
            Name = subscriptionDto.Name,
            Price = subscriptionDto.Amount,
            Interval = subscriptionDto.Interval,
            Description = subscriptionDto.Description,
            PaystackPlanCode = planCode
        };
        await _subscriptionPlanRepo.Create(plan);
        return new BaseResponse()
        {
            Status = true,
            Message = "Plan Successfully Created"
        };
    }

    public async Task<SubscriptionPlanResponseModel> GetAllPlansAsync()
    {
        var plans = await _subscriptionPlanRepo.GetAll();
        if (plans != null)
        {
            return new SubscriptionPlanResponseModel()
            {
                Data = plans.Select(x => new SubscriptionDto()
                {
                    Name = x.Name,
                    Price = x.Price,
                    Interval = x.Interval,
                    NoOfPosts = x.NoOfPosts,
                    Description = x.Description
                }).ToList(),
                Status = true
            };
        }
        return new SubscriptionPlanResponseModel()
        {
            Status = false,
            Data = null
        };
    }

    public async Task<UserSubscriptionResponseModel> GetUserSubscriptionsAsync(int userId)
    {
        var userSubscription = await _userSubscriptionRepo.GetUserSubscriptionsAsync(userId);
        if (userSubscription != null)
        {
            return new UserSubscriptionResponseModel()
            {
                Status = true,
                Data = userSubscription.Select(x => new UserSubscriptionDto()
                {
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    PaystackCustomerCode = x.PaystackCustomerCode,
                    PaystackSubscriptionCode = x.PaystackSubscriptionCode,
                    IsActive = x.IsActive,
                    NoOfPostsThisMonth = x.NoOfPostsThisMonth,
                    Plan = new SubscriptionDto()
                    {
                        Name = x.Plan.Name,
                        Price = x.Plan.Price,
                        Interval = x.Plan.Interval,
                        Description = x.Plan.Description
                    }
                }).ToList()
            };
        }
        return new UserSubscriptionResponseModel()
        {
            Status = false
        };
    }
    public async Task AutoSubscribeSubscription()
    {
        var users = await _userRepo.GetAll();
        if (users != null)
        {
            foreach (var user in users.Where(x => x.AutoSubscribe == true))
            {
                var userSubscription = await _userSubscriptionRepo.GetByExpression(x => x.UserId == user.Id);
                if(userSubscription != null)
                {
                    var plan = await _subscriptionPlanRepo.GetAll();
                    if(plan != null)
                    {
                        if(userSubscription.LastOrDefault().EndDate < DateTime.UtcNow) await SubscribeUserAsync(user.Id, userSubscription.LastOrDefault().PlanId);
                    } 
                }
            }
            foreach (var user in users.Where(x => x.AutoSubscribe == false))
            {
                var userSubscription = await _userSubscriptionRepo.GetByExpression(x => x.UserId == user.Id);
                if(userSubscription != null)
                {
                    var plan = await _subscriptionPlanRepo.Get(x => x.Name == "Basic");
                    if(plan != null)
                    {
                        if(userSubscription.LastOrDefault().EndDate <= DateTime.UtcNow) await SubscribeUserAsync(user.Id, plan.Id);
                    } 
                }
            }
        }
        else
        {
            
        }
    }
    public async Task<BaseResponse> SubscribeUserAsync(int userId, int planId)
    {
        var plan = await _subscriptionPlanRepo.Get(p => p.Id == planId);
        var user = await _userRepo.Get(x => x.Id == userId);
        if (plan == null && user != null)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Invalid plan."
            };
        }
        var payload = new
        {
            customer = user.Email,
            plan = plan.PaystackPlanCode
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.paystack.co/subscription", content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Failed to subscribe user: {result}" };

        var json = JsonDocument.Parse(result);
        var data = json.RootElement.GetProperty("data");
        var subscriptionCode = data.GetProperty("subscription_code").GetString();
        var customerCode = data.GetProperty("customer").GetProperty("customer_code").GetString();
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            PaystackSubscriptionCode = subscriptionCode,
            PaystackCustomerCode = customerCode,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.Interval),
            IsActive = true
        };
        await _userSubscriptionRepo.Create(subscription);
        await _emailService.SendEmailAsync(user.Email, "Subscription Successful", $"You have successfully subscribed to the {plan.Name} plan.");
        return new BaseResponse()
        {
            Status = true,
            Message = "Subscription successful."
        };
    }
}