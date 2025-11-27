using Microsoft.EntityFrameworkCore;
using FullPost.Context;
using FullPost.Entities;
using FullPost.Interfaces.Respositories;

namespace FullPost.Implementations.Respositories;

public class AnalyticsRepo : BaseRepository<Analytic>, IAnalyticsRepo
{
    public AnalyticsRepo(FullPostContext _context)
    {
        context = _context;
    }
}

