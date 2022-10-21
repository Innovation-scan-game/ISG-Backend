using IsolatedFunctions.DTO.UserDTOs;

namespace InnovationGameTests.DTOs;

public class LoginResultDto
{
    public string AccessToken { get; set; }
    public UserDto User { get; set; }
}
