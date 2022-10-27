using Newtonsoft.Json;

namespace IsolatedFunctions.DTO.SignalDTOs;

public class ChangeReadinessDto
{
    [JsonRequired] public bool Ready { get; set; }
}
