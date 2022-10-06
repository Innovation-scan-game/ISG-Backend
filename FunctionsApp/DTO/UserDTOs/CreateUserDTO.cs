using Newtonsoft.Json;

namespace FunctionsApp.DTO.UserDTOs;

public class CreateUserDTO
{
    [JsonRequired] public string Username;

    [JsonRequired] public string Password;

    [JsonRequired] public string Email;
}
