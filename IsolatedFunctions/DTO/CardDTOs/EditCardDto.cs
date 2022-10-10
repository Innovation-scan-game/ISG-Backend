using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.CardDTOs;

public class EditCardDto : CreateCardDto
{
    [JsonRequired] public string Id { get; set; } = "";
}
