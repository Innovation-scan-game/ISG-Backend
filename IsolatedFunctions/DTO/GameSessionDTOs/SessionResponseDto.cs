using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class SessionResponseDto
{
    public string Id { get; set; } = "";
    public int CardNumber { get; set; }
    public UserDto User { get; set; } = null!;
    public string Response { get; set; } = "";
    public DateTime CreatedAt { get; set; }

}
