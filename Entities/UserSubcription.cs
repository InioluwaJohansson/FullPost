namespace FullPost.Entities;
public class UserSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!; // Could be linked to your auth user
    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = default!;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PaystackSubscriptionCode { get; set; }
    public string? PaystackCustomerCode { get; set; }
}