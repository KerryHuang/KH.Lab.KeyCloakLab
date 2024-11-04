using KH.Lab.KeyCloakLab.Models;
using KH.Lab.KeyCloakLab.Services;
using Microsoft.AspNetCore.Mvc;

namespace KH.Lab.KeyCloakLab.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly KeycloakService _keycloakService;

    public ClientsController(KeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _keycloakService.GetClientsAsync();
        return Ok(clients);
    }

    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetClient(string clientId)
    {
        var client = await _keycloakService.GetClientAsync(clientId);
        if (client == null) return NotFound();
        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] Client client)
    {
        await _keycloakService.CreateClientAsync(client);
        return CreatedAtAction(nameof(GetClient), new { clientId = client.ClientId }, client);
    }

    [HttpPut("{clientId}")]
    public async Task<IActionResult> UpdateClient(string clientId, [FromBody] Client client)
    {
        await _keycloakService.UpdateClientAsync(clientId, client);
        return NoContent();
    }

    [HttpDelete("{clientId}")]
    public async Task<IActionResult> DeleteClient(string clientId)
    {
        await _keycloakService.DeleteClientAsync(clientId);
        return NoContent();
    }
}
