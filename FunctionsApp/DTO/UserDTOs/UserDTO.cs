using Domain.Enums;

namespace FunctionsApp.DTO.UserDTOs;

public class UserDTO
{
    public string Id;
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}
