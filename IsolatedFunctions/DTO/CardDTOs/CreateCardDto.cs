using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.CardDTOs;

public class CreateCardDto
{
    [JsonRequired] public string Name { get; set; } = "";
    [JsonRequired] public string Body { get; set; } = "";
    [JsonRequired] public int Type { get; set; }
}
