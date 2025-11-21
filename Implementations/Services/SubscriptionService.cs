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
        if(subscriptionDto.PlanType == SubscriptionPlans.Premium) savedNoOfPosts = int.MaxValue;
        var plan = new SubscriptionPlan
        {
            Name = subscriptionDto.Name,
            Price = subscriptionDto.Amount,
            Interval = subscriptionDto.Interval,
            Description = subscriptionDto.Description,
            PaystackPlanCode = planCode,
            PlanType = subscriptionDto.PlanType,
            NoOfPosts = savedNoOfPosts,
        };
        await _subscriptionPlanRepo.Create(plan);
        return new BaseResponse()
        {
            Status = true,
            Message = "Plan Successfully Created"
        };
    }
    public async Task<BaseResponse> UpdateLocalPlanAsync(UpdateSubscriptionDto updateSubscriptionDto)
    {
        var existingPlan = await _subscriptionPlanRepo.Get(p => p.Id == updateSubscriptionDto.Id);
        if (existingPlan == null)
        {
            return new BaseResponse
            {
                Status = false,
                Message = "Plan not found."
            };
        }
        existingPlan.Description = updateSubscriptionDto.Description;
        existingPlan.Price = updateSubscriptionDto.Amount;
        if (existingPlan.PlanType == SubscriptionPlans.Premium)
            existingPlan.NoOfPosts = int.MaxValue;
        else
            existingPlan.NoOfPosts = updateSubscriptionDto.NoOfPosts;
        await _subscriptionPlanRepo.Update(existingPlan);
        return new BaseResponse
        {
            Status = true,
            Message = "Local plan updated successfully."
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
                    Id = x.Id,
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
            Message ="Unable to retrieve subscriptions.",
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
        var user = await _userRepo.Get(u => u.Id == userId && u.IsDeleted == false);
        if (plan == null || user == null) return null;
        var requestPayload = new
        {
            email = user.Email,
            amount = (int)(plan.Price * 100),
            metadata = new
            {
                plan_id = plan.Id,
                user_id = user.Id
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
    public async Task OnInitialSubscriptionPaid(dynamic data)
    {
        try
        {
            Console.WriteLine(data.data);
            var customerEmail = (string)data.data.customer.email;
            var subscriptionCode = (string)data.data.authorization?.authorization_code;
            //var emailToken = (string)data.data.subscription.email_token ?? "";
            var planId = (int)data.data.metadata.plan_id;
            var userId = (int)data.data.metadata.user_id;
            var user = await _userRepo.Get(u => u.Email == customerEmail && u.Id == userId);
            if (user == null)
            {
                Console.WriteLine("Webhook initial payment: User not found for email " + customerEmail);
                return;
            }
            var plan = await _subscriptionPlanRepo.Get(p => p.Id == planId);
            if (plan == null)
            {
                Console.WriteLine("Webhook initial payment: Plan not found for plan code " + planId);
                return;
            }
            var userSubs = await _userSubscriptionRepo.GetUserSubscriptionsAsync(user.Id);
            if(userSubs != null){
                foreach (var subs in userSubs)
                {
                    subs.IsActive = false;
                    await _userSubscriptionRepo.Update(subs);
                }
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
                PaystackEmailToken = null,
                Email = user.Email,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(days),
                NoOfPostsThisMonth = 0,
                IsActive = true,
                IsDeleted = false
            };
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
                    IsActive = true,
                    IsDeleted = false,
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
    public async Task<bool> ChargeSubscription(string subscriptionCode, string email, decimal amount)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _paystackSecretKey);
        var body = new
        {
            authorization_code = subscriptionCode,
            email = email,
            amount = (int)amount*100
        };
        var response = await client.PostAsJsonAsync("https://api.paystack.co/transaction/charge_authorization", body);
        var json = await response.Content.ReadAsStringAsync();

        dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

        if (result.status == true) return true;
        return false;
    }
    public async Task<BaseResponse> CancelUserSubscriptionAsync(int userId, int subId)
    {
        var user = await _userRepo.Get(u => u.Id == userId);
        var userSub = await _userSubscriptionRepo.Get(x => x.Id == subId && x.IsDeleted == false);
        if (userSub != null  && user != null)
        {
            userSub.IsActive = false;
            userSub.EndDate = DateTime.UtcNow;
            await _userSubscriptionRepo.Update(userSub);
            await _emailService.SendEmailAsync(user.Email, "Subscription Cancelled", "Your subscription has been successfully cancelled.");
            return new BaseResponse { Status = true, Message = "Subscription cancelled successfully." }; 
        }
        return new BaseResponse { Status = false, Message = "User has no active subscription." };
    }
    public async Task<AutoSubscribeResponseModel> EnableCancelUserAutoSubscribe(int userId)
    {
        var user = await _userRepo.Get(u => u.Id == userId);
        if(user != null)
        {
            if(user.AutoSubscribe == true)
            {
                user.AutoSubscribe = false;
                await _userRepo.Update(user);
                return new AutoSubscribeResponseModel { currentStatus = user.AutoSubscribe,  Status = true, Message = "Auto subscription disabled" };
            } 
            if(user.AutoSubscribe == false)
            {
                user.AutoSubscribe = true;
                await _userRepo.Update(user);
                return new AutoSubscribeResponseModel { currentStatus = user.AutoSubscribe, Status = true, Message = "Auto subscription enabled." };
            }  
        }
        return new AutoSubscribeResponseModel { Status = false, Message = "User cannot be found." };
    }
    public async Task CheckRenewals()
    {
        var dueSubscriptions = await _userSubscriptionRepo.GetByExpression(x => x.IsActive == true && x.Plan.Name != "Basic" && x.IsDeleted == false);
        if(dueSubscriptions != null)
        {
            foreach(var userSub in dueSubscriptions)
            {
                if(DateTime.UtcNow >= userSub.EndDate)
                {
                    var user = await _userRepo.Get(x => x.Id == userSub.UserId && x.IsDeleted == false);
                    var plan = await _subscriptionPlanRepo.Get(p => p.Id == userSub.PlanId);
                    if (plan != null && user != null && user.AutoSubscribe == true)
                        await ChargeSubscription(userSub.PaystackSubscriptionCode, user.Email, plan.Price);
                }
            }
        }
    }
    public async Task ResetMonthlyPostCountAsync()
    {
        var subscriptions = await _userSubscriptionRepo.GetByExpression(x => x.IsActive == true && x.IsDeleted == false);
        if (subscriptions == null || !subscriptions.Any())
            return;
        foreach (var sub in subscriptions)
        {
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
                    var user = await _userRepo.Get(x => x.Id == userSub.UserId && x.IsDeleted == false);
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
                            IsActive = true,
                            IsDeleted = false,
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