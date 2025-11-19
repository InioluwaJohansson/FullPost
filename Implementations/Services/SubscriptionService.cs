using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FullPost.Entities;
using FullPost.Implementations.Respositories;
using FullPost.Interfaces.Respositories;
using FullPost.Interfaces.Services;
using FullPost.Models.DTOs;
using FullPost.Models.Enums;
namespace FullPost.Implementations.Services;
public class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _paystackSecretKey;
    private readonly ISubscriptionPlanRepo _subscriptionPlanRepo;
    private readonly IUserSubscriptionRepo _userSubscriptionRepo;
    private readonly IUserRepo _userRepo;
    private readonly IEmailService _emailService;
    private readonly ICustomerService _customerService;

    public SubscriptionService(ISubscriptionPlanRepo subscriptionPlanRepo, IUserSubscriptionRepo userSubscriptionRepo, IConfiguration config, IUserRepo userRepo, IEmailService emailService, ICustomerService customerService)
    {
        _subscriptionPlanRepo = subscriptionPlanRepo;
        _userSubscriptionRepo = userSubscriptionRepo;
        _userRepo = userRepo;
        _httpClient = new HttpClient();
        _emailService = emailService;
        _customerService = customerService;
        _paystackSecretKey = config["Paystack:SecretKey"]!;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSecretKey);
    }
    public async Task<BaseResponse> CancelUserSubscriptionAsync(int userId, int subId)
    {
        var userSub = await _userSubscriptionRepo.Get(x => x.Id == subId);
        if (userSub == null)
            return new BaseResponse { Status = false, Message = "User has no active subscription." };
        if (string.IsNullOrWhiteSpace(userSub.PaystackSubscriptionCode) ||  string.IsNullOrWhiteSpace(userSub.PaystackEmailToken))
            return new BaseResponse { Status = false, Message = "Missing Paystack subscription credentials." };
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
            await _emailService.SendEmailAsync(user.Email, "Subscription Cancelled", "Your subscription has been successfully cancelled.");
        return new BaseResponse { Status = true, Message = "Subscription cancelled successfully." };
    }
    public async Task<BaseResponse> CreatePlanAsync(CreateSubscriptionDto subscriptionDto)
    {
        string paystackInterval;
        if (subscriptionDto.Interval == SubscriptionInterval.Monthly)
            paystackInterval = "monthly";
        else if (subscriptionDto.Interval == SubscriptionInterval.Yearly)
            paystackInterval = "annually";
        else
            return new BaseResponse { Status = false, Message = "Invalid subscription interval." };
        var payload = new
        {
            name = subscriptionDto.Name,
            amount = (int)(subscriptionDto.Amount * 100),
            interval = paystackInterval,
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
        int savedNoOfPosts = subscriptionDto.NoOfPosts;
        if(subscriptionDto.PlanType == SubscriptionPlans.Premium)
        {
            savedNoOfPosts = int.MaxValue;
        }
        var plan = new SubscriptionPlan
        {
            Name = subscriptionDto.Name,
            Price = subscriptionDto.Amount,
            Interval = subscriptionDto.Interval,
            Description = subscriptionDto.Description,
            PaystackPlanCode = planCode,
            PlanType = subscriptionDto.PlanType,
            NoOfPosts = savedNoOfPosts
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
        var newPlans = new List<SubscriptionDto>();
        if (plans != null)
        {
            var basicPlans = plans.Where(x => x.PlanType == SubscriptionPlans.Basic).OrderBy(x => x.Price).ToList();
            newPlans.Add(new SubscriptionDto(){
                MonthlyId = basicPlans.First().Id,
                YearlyId = basicPlans.Last().Id,
                Name = basicPlans.First().Name,
                Description = basicPlans.First().Description,
                MonthlyPrice = basicPlans.First().Price,
                YearlyPrice = basicPlans.Last().Price,
                NoOfPosts = basicPlans.First().NoOfPosts,
            });
            var standardPlans = plans.Where(x => x.PlanType == SubscriptionPlans.Standard).OrderBy(x => x.Price).ToList();
            newPlans.Add(new SubscriptionDto(){
                MonthlyId = standardPlans.First().Id,
                YearlyId = standardPlans.Last().Id,
                Name = standardPlans.First().Name,
                Description = standardPlans.First().Description,
                MonthlyPrice = standardPlans.First().Price,
                YearlyPrice = standardPlans.Last().Price,
                NoOfPosts = standardPlans.First().NoOfPosts,
            });
            var premiumPlans = plans.Where(x => x.PlanType == SubscriptionPlans.Premium).OrderBy(x => x.Price).ToList();
            newPlans.Add(new SubscriptionDto(){
                MonthlyId = premiumPlans.First().Id,
                YearlyId = premiumPlans.Last().Id,
                Name = premiumPlans.First().Name,
                Description = premiumPlans.First().Description,
                MonthlyPrice = premiumPlans.First().Price,
                YearlyPrice = premiumPlans.Last().Price,
                NoOfPosts = premiumPlans.First().NoOfPosts,
            });
            return new SubscriptionPlanResponseModel()
            {
                Data = newPlans.OrderByDescending(x => x.NoOfPosts).ToList(),
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
                    Plan = new ShortUserSubscriptionDto()
                    {
                        Name = x.Plan.Name,
                        Price = x.Plan.Price,
                        Interval = x.Plan.Interval,
                        Description = x.Plan.Description,
                        NoOfPosts = x.Plan.NoOfPosts,
                        Id = x.Plan.Id
                    }
                }).ToList()
            };
        }
        return new UserSubscriptionResponseModel()
        {
            Status = false
        };
    }
    public async Task<AdminSubscriptionPlanResponseModel> GetAdminSubscriptionsAsync()
    {
        var subscriptionPlans = await _subscriptionPlanRepo.GetAll();
        if (subscriptionPlans != null)
        {
            return new AdminSubscriptionPlanResponseModel()
            {
                Status = true,
                Data = subscriptionPlans.Select(x => new ShortUserSubscriptionDto()
                {
                    Name = x.Name,
                    Price = x.Price,
                    Interval = x.Interval,
                    Description = x.Description,
                    NoOfPosts = x.NoOfPosts,
                    PlanType = x.PlanType,
                    Id = x.Id
                }).ToList()
            };
        }
        return new AdminSubscriptionPlanResponseModel()
        {
            Status = false
        };
    }
    public async Task<string?> GenerateSubscriptionPaymentLink(int userId, int planId)
    {
        var plan = await _subscriptionPlanRepo.Get(p => p.Id == planId);
        var user = await _userRepo.Get(u => u.Id == userId);
        if (plan == null || user == null) return null;
        var requestPayload = new
        {
            email = user.Email,
            amount = (int)(plan.Price * 100),
            metadata = new
            {
                plan_id = planId,
                user_id = userId
            }
        };
        var requestJson = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.paystack.co/transaction/initialize", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return null;

        var json = JsonDocument.Parse(responseContent);
        var authUrl = json.RootElement.GetProperty("data").GetProperty("authorization_url").GetString();
        return authUrl;
    }
    public async Task<BaseResponse> SubscribeUserAsync(int userId, int planId, string paystackReference)
    {
        var plan = await _subscriptionPlanRepo.Get(p => p.Id == planId);
        var user = await _userRepo.Get(x => x.Id == userId);

        if (user == null)
            return new BaseResponse { Status = false, Message = "User not found." };

        if (plan == null)
            return new BaseResponse { Status = false, Message = "Invalid plan selected." };

        // 1. Verify the payment from Paystack
        var verifyResponse = await _httpClient.GetAsync(
            $"https://api.paystack.co/transaction/verify/{paystackReference}");

        var verifyResultString = await verifyResponse.Content.ReadAsStringAsync();
        if (!verifyResponse.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Payment verification failed: {verifyResultString}" };
        var verifyJson = JsonDocument.Parse(verifyResultString);
        var status = verifyJson.RootElement.GetProperty("data").GetProperty("status").GetString();
        if (status != "success")
            return new BaseResponse { Status = false, Message = "Payment not successful." };
        var payload = new
        {
            customer = user.Email,
            plan = plan.PaystackPlanCode
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var subscriptionResponse = await _httpClient.PostAsync("https://api.paystack.co/subscription", content);
        var subscriptionResult = await subscriptionResponse.Content.ReadAsStringAsync();
        if (!subscriptionResponse.IsSuccessStatusCode)
            return new BaseResponse { Status = false, Message = $"Failed to create subscription: {subscriptionResult}" };
        var subscriptionJson = JsonDocument.Parse(subscriptionResult);
        var data = subscriptionJson.RootElement.GetProperty("data");
        var subscriptionCode = data.GetProperty("subscription_code").GetString();
        var emailToken = data.GetProperty("email_token").GetString();
        int addDays = plan.Interval == SubscriptionInterval.Monthly ? 30 : 365;
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            PaystackSubscriptionCode = subscriptionCode,
            PaystackEmailToken = emailToken,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(addDays),
            IsActive = true,
            Email = user.Email,
            NoOfPostsThisMonth = 0
        };

        await _userSubscriptionRepo.Create(subscription);
        user.AutoSubscribe = true;
        user.SubscriptionPlan = plan.Name switch
        {
            "Basic" => SubscriptionPlans.Basic,
            "Standard" => SubscriptionPlans.Standard,
            "Premium" => SubscriptionPlans.Premium,
            _ => user.SubscriptionPlan
        };
        await _userRepo.Update(user);
        await _emailService.SendEmailAsync(user.Email,"Subscription Activated",$"Your subscription to the {plan.Name} plan is active.");
        return new BaseResponse
        {
            Status = true,
            Message = "Subscription activated successfully."
        };
    }
    public async Task OnInitialSubscriptionPaid(dynamic data)
    {
        try
        {
            var customerEmail = (string)data.data.customer.email;
            var subscriptionCode = (string)data.data.subscription.subscription_code;
            var emailToken = (string)data.data.subscription.email_token;
            var planCode = (string)data.data.plan.plan_code;

            // Fetch user
            var user = await _userRepo.Get(u => u.Email == customerEmail);
            if (user == null)
            {
                Console.WriteLine("Webhook initial payment: User not found for email " + customerEmail);
                return;
            }

            // Fetch plan from your DB using Paystack plan code
            var plan = await _subscriptionPlanRepo.Get(p => p.PaystackPlanCode == planCode);
            if (plan == null)
            {
                Console.WriteLine("Webhook initial payment: Plan not found for plan code " + planCode);
                return;
            }

            int days = plan.Interval switch
            {
                SubscriptionInterval.Monthly => 30,
                SubscriptionInterval.Yearly => 365,
                _ => 30
            };
            var subscription = new UserSubscription
            {
                UserId = user.Id,
                PlanId = plan.Id,
                PaystackSubscriptionCode = subscriptionCode,
                PaystackEmailToken = emailToken,
                Email = user.Email,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(days),
                NoOfPostsThisMonth = 0,
                IsActive = true
            };
            user.AutoSubscribe = true;
            user.SubscriptionPlan = plan.Name switch
            {
                "Basic" => SubscriptionPlans.Basic,
                "Standard" => SubscriptionPlans.Standard,
                "Premium" => SubscriptionPlans.Premium,
                _ => SubscriptionPlans.Basic
            };
            await _userSubscriptionRepo.Create(subscription);
            await _userRepo.Update(user);
            await _emailService.SendEmailAsync(user.Email,"Subscription Activated",$"Your subscription to the {plan.Name} plan has been successfully activated.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing initial subscription payment webhook: {ex}");
        }
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
                int addDays;
                if (plan.Interval == SubscriptionInterval.Monthly)
                    addDays = 30;
                else
                    addDays = 365;
                var subscription = new UserSubscription
                {
                    UserId = userSub.UserId,
                    PlanId = plan.Id,
                    PaystackSubscriptionCode = subscriptionCode,
                    PaystackEmailToken = userSub.PaystackEmailToken,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(addDays),
                    NoOfPostsThisMonth = 0,
                    NextResetDate = DateTime.UtcNow.AddDays(30),
                    Email = user.Email,
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
            user.SubscriptionPlan = SubscriptionPlans.Basic;
            await _customerService.CreatePlanForNewUser(user.Id, user.Email);
            await _userSubscriptionRepo.Update(userSub);
            if (user != null && plan != null)    await _emailService.SendEmailAsync(user.Email,$"{plan.Name} Subscription Payment Failed",$"We could not process your subscription payment. Please update your payment method to continue your subscription and avoid service disruption.");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnSubscriptionPaymentFailed error: {ex}");
        }
    }
    public async Task ResetMonthlyPostCountAsync()
    {
        var subscriptions = await _userSubscriptionRepo.GetAll();
        if (subscriptions == null || !subscriptions.Any())
            return;
        foreach (var sub in subscriptions)
        {
            if (!sub.IsActive) continue;
            if (DateTime.UtcNow >= sub.NextResetDate && sub.EndDate > sub.NextResetDate)
            {
                sub.NoOfPostsThisMonth = 0;
                sub.NextResetDate = sub.NextResetDate.AddDays(30);
                await _userSubscriptionRepo.Update(sub);
            }else if (DateTime.UtcNow >= sub.EndDate)
            {
                sub.IsActive = false;
                await _userSubscriptionRepo.Update(sub);
            }
        }
    }
    public async Task ResetToBasic()
    {
        var userSubscriptions = await _userSubscriptionRepo.GetByExpression(x => x.IsActive == true);
        if(userSubscriptions != null)
        {
            foreach(var userSub in userSubscriptions)
            {
                if(DateTime.UtcNow >= userSub.EndDate.AddDays(2))
                {
                    userSub.IsActive = false;
                    await _userSubscriptionRepo.Update(userSub);
                    var plan = await _subscriptionPlanRepo.Get(p => p.Name == "Basic");
                    var user = await _userRepo.Get(x => x.Id == userSub.UserId);
                    if (plan != null & user != null)
                    {
                        int addDays;
                        if (plan.Interval == SubscriptionInterval.Monthly)
                            addDays = 30;
                        else
                            addDays = 365;
                        var subscription = new UserSubscription
                        {
                            UserId = userSub.UserId,
                            PlanId = plan.Id,
                            PaystackSubscriptionCode = plan.PaystackPlanCode,
                            PaystackEmailToken = userSub.PaystackEmailToken,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(addDays),
                            NoOfPostsThisMonth = 0,
                            NextResetDate = DateTime.UtcNow.AddDays(30),
                            Email = user.Email,
                            IsActive = true
                        };
                        await _userSubscriptionRepo.Create(subscription);
                        user.SubscriptionPlan = SubscriptionPlans.Basic;
                        await _userRepo.Update(user);
                    }
                }
            }
        }
    }
}