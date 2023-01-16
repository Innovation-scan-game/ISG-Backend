using System.Net;
using System.Security.Claims;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class UserService : IUserService {
    private readonly InnovationGameDbContext _context;

    public UserService(InnovationGameDbContext context) {
        _context = context;
    }

    public async Task<User?> GetUser(Guid id) {
        return await _context.Users.Include(usr => usr.CurrentSession!.Responses).Include(usr => usr.CurrentSession!.Cards)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public IQueryable<User> GetUsersInSession(Guid sessionId) {
        return _context.Users.Include(usr => usr.CurrentSession).Where(u => u.CurrentSession!.Id == sessionId);
    }

    public async Task<User?> GetExistingUser(string username, string email) {
        return await _context.Users.FirstOrDefaultAsync(u => u.Name == username || u.Email == email);
    }

    public async Task<bool> CheckUserAllowAdminChange(ClaimsPrincipal? principal) {
        var dbUser = await GetUserByName(principal?.Identity?.Name!);
        return dbUser is not null && dbUser.Role == UserRoleEnum.Admin;
    }

    public async Task<User?> CheckUserLoggedIn(ClaimsPrincipal principal) {
        User? loggedInUser = await GetUserByName(principal?.Identity?.Name!);
        return loggedInUser;
    }

    public async Task<List<User>> GetAllUsers() {
        return await _context.Users.Include(usr => usr.CurrentSession).ToListAsync();
    }

    public async Task<User?> GetUserByEmail(string email) {
        return await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByName(string name) {
        return await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Name == name);
    }

    public Task AddUser(User user) {
        _context.Users.Add(user);
        return _context.SaveChangesAsync();
    }

    public async Task DeleteUser(Guid id) {
        User? user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user is not null) {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateUser(User user) {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
