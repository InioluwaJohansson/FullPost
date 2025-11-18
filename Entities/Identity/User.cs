using FullPost.Contracts;
using FullPost.Entities;
using FullPost.Models.Enums;

namespace FullPost.Entities.Identity;

public class User : AuditableEntity
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool AutoSubscribe { get; set; }
    public SubscriptionPlans SubscriptionPlan { get; set; }
    public Customer Customer { get; set; }
}
