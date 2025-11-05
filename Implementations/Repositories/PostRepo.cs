using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

namespace FullPost.Implementations.Respositories;

public class PostRepo : BaseRepository<Post>, IPostRepo
{
    public PostRepo(FullPostContext _context)
    {
        context = _context;
    }
}

