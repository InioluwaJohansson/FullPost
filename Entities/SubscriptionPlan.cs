using FullPost.Models.Enums;

namespace FullPost.Entities;
public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Interval { get; set; }
    public int NoOfPosts { get; set; }
    public string? Description { get; set; }
    public string? PaystackPlanCode { get; set; }
    public SubscriptionPlans PlanType { get; set; } 
}   