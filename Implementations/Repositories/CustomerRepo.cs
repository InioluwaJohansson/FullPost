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
}

