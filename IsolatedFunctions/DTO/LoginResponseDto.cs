using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.DTO;

public class LoginResponseDto
{
    public UserDto User { get; set; } = null!;
    public string AuthToken { get; set; } = "";
}
