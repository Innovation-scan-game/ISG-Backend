using DAL.Data;
using Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class UserService : IUserService
{
    private readonly InnovationGameDbContext _context;
    private readonly IValidator<User> _validator;

    public UserService(InnovationGameDbContext context, IValidator<User> validator)

    {
        _context = context;
        _validator = validator;
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

    public async Task AddUser(User user)
    {
        if (await GetUserByEmail(user.Email) != null)
        {
            throw new Exception("Email already exists");
        }

        var validation = await _validator.ValidateAsync(user);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUser(Guid id)
    {
        User? user = _context.Users.FirstOrDefault(u => u.Id == id);

        if (user is not null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return;
        }

        throw new Exception("User not found.");
    }

    public async Task UpdateUser(User user)
    {
        if (await GetUser(user.Id) is null)
        {
            throw new Exception("User not found.");
        }

        var validation = await _validator.ValidateAsync(user);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
