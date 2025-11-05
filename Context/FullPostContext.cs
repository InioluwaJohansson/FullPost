using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FullPost.Entities;
using FullPost.Entities.Identity;
using FullPost.Entities;

namespace FullPost.Context;

public class FullPostContext: DbContext
{
    public FullPostContext(DbContextOptions<FullPostContext> optionsBuilder): base(optionsBuilder)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Email> Email { get; set; }
}
