using FullPost.Models.Enums;

namespace FullPost.Models.DTOs;
public class CreateSubscriptionDto
{
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public SubscriptionInterval Interval { get; set; }
    public int NoOfPosts { get; set; }
    public string? Description { get; set; }
    public SubscriptionPlans PlanType { get; set; }
}
public class UpdateSubscriptionDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public int NoOfPosts { get; set; }
    public string? Description { get; set; }
}
public class ShortUserSubscriptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public SubscriptionInterval Interval { get; set; }
    public int NoOfPosts { get; set; }
    public string? Description { get; set; }
    public SubscriptionPlans PlanType { get; set; }
}
public class SubscriptionDto
{
    public int MonthlyId { get; set; }
    public int YearlyId { get; set; }
    public string Name { get; set; } 
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int NoOfPosts { get; set; }
    public string? Description { get; set; }
}
public class UserSubscriptionDto
{
    public int Id { get; set; }
    public ShortUserSubscriptionDto Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int NoOfPostsThisMonth { get; set; }
    public string? PaystackSubscriptionCode { get; set; }
    public string? PaystackCustomerCode { get; set; }
}
public class AutoSubscribeResponseModel : BaseResponse
{
    public bool currentStatus { get; set; }
}
public class UserSubscriptionResponseModel : BaseResponse
{
    public bool currentStatus { get; set; }
    public ICollection<UserSubscriptionDto> Data { get; set; } = new HashSet<UserSubscriptionDto>();
}
public class SubscriptionPlanResponseModel : BaseResponse
{
    public ICollection<SubscriptionDto> Data { get; set; } = new HashSet<SubscriptionDto>();
}
public class AdminSubscriptionPlanResponseModel : BaseResponse
{
    public ICollection<ShortUserSubscriptionDto> Data { get; set; } = new HashSet<ShortUserSubscriptionDto>();
}