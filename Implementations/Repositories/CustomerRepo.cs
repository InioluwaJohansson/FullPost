using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

namespace FullPost.Implementations.Respositories;

public class CustomerRepo : BaseRepository<Customer>, ICustomerRepo
{
    public CustomerRepo(FullPostContext _context)
    {
        context = _context;
    }
    public async Task<Customer?> GetById(int userId)
    {
        return await context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == userId);
    }
}

