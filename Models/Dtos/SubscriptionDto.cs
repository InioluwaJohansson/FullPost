namespace FullPost.Models.DTOs;
public class SubscriptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string Interval { get; set; } = default!;
    public string? Description { get; set; }
}
public class UserSubscriptionDto
{
    public int Id { get; set; }
    public SubscriptionDto Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? PaystackSubscriptionCode { get; set; }
    public string? PaystackCustomerCode { get; set; }
}
public class UserSubscriptionResponseModel : BaseResponse
{
    public ICollection<UserSubscriptionDto> Data { get; set; } = new HashSet<UserSubscriptionDto>();
}
public class SubscriptionPlanResponseModel : BaseResponse
{
    public ICollection<SubscriptionDto> Data { get; set; } = new HashSet<SubscriptionDto>();
}