using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class JoinRequestDto
{
    [JsonRequired] public string SessionAuth { get; set; } = "";
}
