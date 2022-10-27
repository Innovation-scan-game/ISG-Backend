using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.SignalDTOs;

public class ChatMessageDto
{
    [JsonRequired] public string Message { get; set; } = "";
}
