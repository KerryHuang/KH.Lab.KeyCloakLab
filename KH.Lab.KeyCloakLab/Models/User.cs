namespace KH.Lab.KeyCloakLab.Models;

public class User
{
    public string Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public Dictionary<string, List<string>>? Attributes { get; set; }
}
