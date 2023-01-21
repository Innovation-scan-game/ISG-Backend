using System.Security.Claims;
using Domain.Models;

namespace Services.Interfaces;

public interface IUserService {
    Task<User?> GetUser(Guid id);
    Task<List<User>> GetAllUsers();
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserByName(string name);
    Task AddUser(User user);
    Task DeleteUser(Guid id);
    Task UpdateUser(User user);
    IQueryable<User> GetUsersInSession(Guid sessionId);
    Task<User?> GetExistingUser(string username, string email);
    Task<bool> CheckUserAllowAdminChange(ClaimsPrincipal principal);
    Task<User?> CheckUserLoggedIn(ClaimsPrincipal principal);
    Task RemoveUsersFromSession(Guid currentSessionId);
}
