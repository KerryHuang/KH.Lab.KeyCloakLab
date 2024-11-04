namespace KH.Lab.KeyCloakLab.Models;
public class Client
{
    public required string ClientId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required List<string> RedirectUris { get; set; }
    public string ClientAuthenticatorType { get; set; } = "client-secret";
    public string Protocol { get; set; } = "openid-connect";
    public bool PublicClient { get; set; }
    public bool AuthorizationServicesEnabled { get; set; }
    public bool ServiceAccountsEnabled { get; set; }
}
