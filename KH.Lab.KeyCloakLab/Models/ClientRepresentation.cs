namespace KH.Lab.KeyCloakLab.Models;

public class ClientRepresentation
{
    public string ClientId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; } = true;
    // 添加更多屬性以符合您的需求
}
