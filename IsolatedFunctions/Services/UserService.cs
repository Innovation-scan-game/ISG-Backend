using DAL.Data;
using Domain.Models;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Services;

public class UserService : IUserService
{
    private readonly InnovationGameDbContext _context;

    public UserService(InnovationGameDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUser(Guid id)
    {
        return await _context.Users.Include(usr => usr.CurrentSession!.Responses).Include(usr => usr.CurrentSession!.Cards)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public IQueryable<User> GetUsersInSession(Guid sessionId)
    {
        return _context.Users.Include(usr => usr.CurrentSession).Where(u => u.CurrentSession!.Id == sessionId);
    }

    public async Task<User?> GetExistingUser(string username, string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Name == username || u.Email == email);
    }


    public async Task<List<User>> GetAllUsers()
    {
        return await _context.Users.Include(usr => usr.CurrentSession).ToListAsync();
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByName(string name)
    {
        return await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Name == name);
    }

    public Task AddUser(User user)
    {
        _context.Users.Add(user);
        return _context.SaveChangesAsync();
    }

    public async Task DeleteUser(Guid id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateUser(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
