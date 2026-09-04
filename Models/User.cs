// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

namespace SimpleChat.Models;

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
