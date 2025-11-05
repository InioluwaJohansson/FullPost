using FullPost.Entities;
namespace FullPost.Interfaces.Respositories;

public interface IUserSubscriptionRepo : IRepo<UserSubscription>
{
    Task<IList<UserSubscription>> GetUserSubscriptionsAsync(int userId);
}