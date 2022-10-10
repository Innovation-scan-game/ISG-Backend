using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.DTO;

//TODO: Remove
public class LoginResponseDto
{
    public UserDto User { get; set; }
    public string AuthToken { get; set; } = "";
}
