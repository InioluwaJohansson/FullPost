namespace FullPost.Entities;
public class UserSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public int NoOfPostsThisMonth { get; set; }
    public SubscriptionPlan Plan { get; set; } = default!;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PaystackSubscriptionCode { get; set; }
    public string? PaystackCustomerCode { get; set; }
}