using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class SessionDto
{
    public string Id { get; set; } = "";
    public DateTime Created { get; set; }
    public string SessionCode { get; set; } = "";
    public List<UserDto> Players { get; set; } = new();
    public string Status { get; set; } = "";
    public List<CardDto> Cards { get; set; } = new();
    public List<SessionResponseDto> Responses { get; set; } = new();

}
