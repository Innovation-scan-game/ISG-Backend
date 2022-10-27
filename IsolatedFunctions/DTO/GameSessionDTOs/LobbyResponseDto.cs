using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class LobbyResponseDto
{
    public Guid Id { get; set; }
    public Guid HostId { get; set; }
    public string SessionCode { get; set; } = "";
    public DateTime Created { get; set; }
    public string SessionAuth { get; set; } = "";
    public LobbyPlayerDto[] Players { get; set; } = Array.Empty<LobbyPlayerDto>();
}
