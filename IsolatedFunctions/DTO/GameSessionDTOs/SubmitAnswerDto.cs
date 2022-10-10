using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class SubmitAnswerDto
{
    [JsonRequired] public string Answer { get; set; } = "";
    [JsonRequired] public string UserId { get; set; } = "";
}
