using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TowFast.Application.Contracts;
using TowFast.Application.Services;

namespace TowFast.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterClientCommand command, CancellationToken ct)
    {
        var response = await authService.RegisterAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public Task<AuthResponse> Login(LoginCommand command, CancellationToken ct) => authService.LoginAsync(command, ct);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public Task<AuthResponse> Refresh(RefreshCommand command, CancellationToken ct) => authService.RefreshAsync(command, ct);

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshCommand command, CancellationToken ct)
    {
        await authService.LogoutAsync(command.RefreshToken, ct);
        return NoContent();
    }
}
