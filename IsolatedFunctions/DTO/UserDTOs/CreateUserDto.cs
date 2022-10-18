using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.UserDTOs;

public class CreateUserDto
{
    [JsonRequired] public string Username { get; set; } = "";

    [JsonRequired] public string Password { get; set; } = "";

    [JsonRequired] public string Email { get; set; } = "";
}
