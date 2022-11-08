using Domain.Models;

namespace Services;

public class UserInfo
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Picture { get; set; }

    public UserInfo(User user)
    {
        Id = user.Id;
        Username = user.Name;
        Email = user.Email;
        Picture = user.Picture;
    }
}
