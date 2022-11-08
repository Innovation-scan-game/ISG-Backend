using IsolatedFunctions.DTO.UserDTOs;

namespace InnovationGameTests.DTOs;

public class LoginResultDto
{
    public string AccessToken { get; set; } = null!;
    public UserDto User { get; set; } = null!;
}
