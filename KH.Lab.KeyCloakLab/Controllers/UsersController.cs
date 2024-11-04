using KH.Lab.KeyCloakLab.Models;
using KH.Lab.KeyCloakLab.Services;
using Microsoft.AspNetCore.Mvc;

namespace KH.Lab.KeyCloakLab.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly KeycloakService _keycloakService;

    public UsersController(KeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _keycloakService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _keycloakService.GetUserAsync(userId);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        await _keycloakService.CreateUserAsync(user);
        return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] User user)
    {
        await _keycloakService.UpdateUserAsync(userId, user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        await _keycloakService.DeleteUserAsync(userId);
        return NoContent();
    }

    [HttpPut("{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request)
    {
        await _keycloakService.ResetPasswordAsync(userId, request.NewPassword, request.Temporary);
        return NoContent();
    }
}
