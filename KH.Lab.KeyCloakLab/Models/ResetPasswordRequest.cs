namespace KH.Lab.KeyCloakLab.Models;

public class ResetPasswordRequest
{
    /// <summary>
    /// 設置的新密碼
    /// </summary>
    public required string NewPassword { get; set; }
    /// <summary>
    /// 如果為 true，則密碼為臨時密碼，用戶在下次登錄時需要更改密碼。
    /// </summary>
    public bool Temporary { get; set; } = false;
}