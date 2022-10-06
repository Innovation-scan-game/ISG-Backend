using Newtonsoft.Json;

namespace FunctionsApp.DTO.GameSessionDTOs;

public class JoinRequestDTO
{
    [JsonRequired] public string SessionAuth { get; set; }
}
