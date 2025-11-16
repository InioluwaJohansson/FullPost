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
    private readonly string _paystackPublicKey;
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
        _paystackPublicKey = config["Paystack:PublicKey"]!;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSecretKey);
    }
    public async Task<(bool, string)> CheckUserSubscriptionStatus(int userId)
    {
        var userSubscription = await _userSubscriptionRepo.GetByExpression(x => x.UserId == userId);
        var subscription = await _subscriptionPlanRepo.Get(x => x.Id == userSubscription.LastOrDefault().PlanId);
        if (userSubscription != null && subscription != null && userSubscription.LastOrDefault().EndDate > DateTime.UtcNow) return (true,subscription.NoOfPosts);
        else return (false, null);
    }
    public async Task<BaseResponse> CancelUserSubscriptionAsync(int userId, int subId)
    {
        var userSub = await _userSubscriptionRepo.Get(x => x.Id == subId);

        if (userSub == null)
            return new BaseResponse { Status = false, Message = "User has no active subscription." };

        if (string.IsNullOrWhiteSpace(userSub.PaystackSubscriptionCode) ||
            string.IsNullOrWhiteSpace(userSub.PaystackEmailToken))
        {
            return new BaseResponse { Status = false, Message = "Missing Paystack subscription credentials." };
        }

        var payload = new
        {
            code = userSub.PaystackSubscriptionCode,
            token = userSub.PaystackEmailToken
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.paystack.co/subscription/disable", content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Failed to cancel subscription: {result}" };

        userSub.IsActive = false;
        userSub.EndDate = DateTime.UtcNow;
        await _userSubscriptionRepo.Update(userSub);

        var user = await _userRepo.Get(u => u.Id == userId);
        if (user != null)
        {
            await _emailService.SendEmailAsync(user.Email, "Subscription Cancelled", "Your subscription has been successfully cancelled.");
        }

        return new BaseResponse { Status = true, Message = "Subscription cancelled successfully." };
    }


    public async Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto)
    {
        var payload = new
        {
            name = subscriptionDto.Name,
            amount = (int)(subscriptionDto.Amount * 100),
            interval = subscriptionDto.Interval.ToString(),
            description = subscriptionDto.Description,
            send_invoices = true
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
                    PaystackCustomerCode = x.PaystackEmailToken,
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
        var emailToken = data.GetProperty("email_token").GetString();
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            PaystackSubscriptionCode = subscriptionCode,
            PaystackEmailToken = emailToken,
            Email = user.Email,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.Interval),
            NoOfPostsThisMonth = 0,
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
    public async Task<BaseResponse> VerifyAndActivateSubscriptionAsync(string reference, int userId, int planId)
    {
        var verifyUrl = $"https://api.paystack.co/transaction/verify/{reference}";
        var response = await _httpClient.GetAsync(verifyUrl);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new BaseResponse
            {
                Status = false,
                Message = $"Verification failed: {json}"
            };
        }

        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        var status = data.GetProperty("status").GetString();
        if (status != "success")
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Payment not successful."
            };
        }

        var user = await _userRepo.Get(x => x.Id == userId);
        if (user == null) return new BaseResponse { Status = false, Message = "User not found." };

        var plan = await _subscriptionPlanRepo.Get(p => p.Id == planId);
        if (plan == null) return new BaseResponse { Status = false, Message = "Invalid plan." };

        // Verify paid amount matches plan price
        var paidAmount = data.GetProperty("amount").GetInt32() / 100;
        if (paidAmount < plan.Price)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Incorrect payment amount."
            };
        }

        string customerCode = data.GetProperty("customer").GetProperty("customer_code").GetString();
        string emailToken = data.GetProperty("customer").GetProperty("email_token").GetString();

        // Some transactions include subscription codes, otherwise you get them from subscription endpoint
        // But Paystack often nests them here:
        string subscriptionCode = data.TryGetProperty("subscription", out var subNode)
            ? subNode.GetProperty("subscription_code").GetString()
            : null;

        if (string.IsNullOrWhiteSpace(subscriptionCode))
        {
            var autoSubscribe = await SubscribeUserAsync(userId, planId);
            return autoSubscribe;
        }

        // Save subscription the same way SubscribeUserAsync does
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            PaystackSubscriptionCode = subscriptionCode,
            PaystackEmailToken = emailToken,
            Email = user.Email,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.Interval),
            NoOfPostsThisMonth = 0,
            IsActive = true
        };

        await _userSubscriptionRepo.Create(subscription);

        await _emailService.SendEmailAsync(
            user.Email,
            $"{plan.Name} Subscription Activated",
            $"Your payment was verified and your  plan is now active."
        );

        return new BaseResponse
        {
            Status = true,
            Message = "Subscription activated successfully."
        };
    }
    public async Task OnSubscriptionRenewed(dynamic data)
    {
        try
        {
            string subscriptionCode = data.subscription_code ?? data.data?.subscription_code;
            string customerEmail = null;
            try { customerEmail = data.customer?.email ?? data.data?.customer?.email; } catch { }

            List<UserSubscription> allSubs = null;
            if (!string.IsNullOrWhiteSpace(subscriptionCode))
            {
                allSubs = (await _userSubscriptionRepo.GetByExpression(x => x.Email == customerEmail && x.PaystackSubscriptionCode == subscriptionCode)).ToList();
            }
            var userSub = allSubs?.LastOrDefault();
            if (userSub == null)  return;
            var user = await _userRepo.Get(u => u.Id == userSub.UserId);
            var plan = await _subscriptionPlanRepo.Get(p => p.Id == userSub.PlanId);
            if (plan != null & user != null)
            {
                foreach(var sub in allSubs)
                {
                    sub.IsActive = false;
                    await _userSubscriptionRepo.Update(sub);
                }
                var subscription = new UserSubscription
                {
                    UserId = userSub.UserId,
                    PlanId = plan.Id,
                    PaystackSubscriptionCode = subscriptionCode,
                    PaystackEmailToken = userSub.PaystackEmailToken,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(plan.Interval),
                    NoOfPostsThisMonth = 0,
                    IsActive = true
                };
                await _userSubscriptionRepo.Create(subscription);
                await _emailService.SendEmailAsync(user.Email,$"{plan.Name} Subscription Renewed",$"Your subscription has been successfully renewed. Your post quota for this billing period has been reset.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnSubscriptionRenewed error: {ex}");
        }
    }
    public async Task OnSubscriptionPaymentFailed(dynamic data)
    {
        try
        {
            string subscriptionCode = data.subscription_code ?? data.data?.subscription_code;
            string customerEmail = null;
            try { customerEmail = data.customer?.email ?? data.data?.customer?.email; } catch { }

            UserSubscription? userSub = null;
            if (!string.IsNullOrWhiteSpace(subscriptionCode))
            {
                var allSubs = await _userSubscriptionRepo.GetByExpression(x => x.Email == customerEmail && x.PaystackSubscriptionCode == subscriptionCode);
                userSub = allSubs?.LastOrDefault();
            }
            if (userSub == null)  return;
            var user = await _userRepo.Get(u => u.Id == userSub.UserId);
            var plan = await _subscriptionPlanRepo.Get(p => p.Id == userSub.PlanId);
            userSub.IsActive = false;
            userSub.EndDate = DateTime.UtcNow;
            await _userSubscriptionRepo.Update(userSub);
            if (user != null && plan != null)    await _emailService.SendEmailAsync(user.Email,$"{plan.Name} Subscription Payment Failed",$"We could not process your subscription payment. Please update your payment method to continue your subscription and avoid service disruption.");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnSubscriptionPaymentFailed error: {ex}");
        }
    }
}