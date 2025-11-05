using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;
using FullPost.Entities.Identity;

namespace FullPost.Implementations.Respositories;

public class UserRepo : BaseRepository<User>, IUserRepo
{
    public UserRepo(FullPostContext _context)
    {
        context = _context;
    }
}

