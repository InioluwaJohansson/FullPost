using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

namespace FullPost.Implementations.Respositories;

public class UserSubscriptionRepo : BaseRepository<UserSubscription>, IUserSubscriptionRepo
{
    public UserSubscriptionRepo(FullPostContext _context)
    {
        context = _context;
    }
    public async Task<IList<UserSubscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await context.UserSubscriptions.Include(x => x.Plan).Where(x => x.UserId == userId).ToListAsync();
    }
}


