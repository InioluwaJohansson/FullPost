namespace FullPost.Entities;
public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string Interval { get; set; } = default!;
    public string? Description { get; set; }
    public string? PaystackPlanCode { get; set; }
}   