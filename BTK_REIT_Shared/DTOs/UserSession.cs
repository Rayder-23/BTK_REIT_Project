namespace BTK_REIT_Shared.DTOs;

public class UserSession
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int SecurityLevel { get; set; }
    public string Token { get; set; } = string.Empty;
}
