using DAL.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Services;

public class UserService
{
    private readonly InnovationGameDbContext _context;

    public UserService(InnovationGameDbContext context)
    {
        _context = context;
    }

    public User? GetUser(Guid id)
    {
        return _context.Users.Include(usr => usr.CurrentSession).FirstOrDefault(u => u.Id == id);
    }

    public List<User> GetAllUsers()
    {
        return _context.Users.Include(usr => usr.CurrentSession).ToList();
    }

    public User? GetUserByEmail(string email)
    {
        return _context.Users.Include(usr => usr.CurrentSession).FirstOrDefault(u => u.Email == email);
    }

    public User? GetUserByName(string name)
    {
        return _context.Users.Include(usr => usr.CurrentSession).FirstOrDefault(u => u.Name == name);
    }

    public async Task DeleteUser(Guid id)
    {
        _context.Users.Remove(_context.Users.Find(id));
        await _context.SaveChangesAsync();
    }
}
