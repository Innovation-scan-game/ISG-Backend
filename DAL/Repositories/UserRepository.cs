using DAL.Data;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class UserRepository : Repository<User>
{
    private DbContext _context;
    public UserRepository(InnovationGameDbContext context) : base(context)
    {
        _context = context;
    }
}
