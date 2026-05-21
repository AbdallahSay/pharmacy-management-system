using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pharmacy.Application.Auth.Commands.Login;
using Pharmacy.Application.Auth.Commands.RefreshToken;
using Pharmacy.Application.Auth.Commands.Register;
using Pharmacy.Application.Auth.Commands.RevokeAllTokens;
using Pharmacy.Application.Auth.Commands.RevokeToken;
using Pharmacy.Application.Auth.Contracts;
using Pharmacy.Application.Auth.DTOs;
using Pharmacy.Application.Common.Constants;
using System.Security.Claims;

namespace Pharmacy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IWebHostEnvironment _env;

    public AuthController(ISender sender, IWebHostEnvironment env)
    {
        _sender = sender;
        _env = env;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("loginPolicy")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            dto.Email, 
            dto.Password, 
            IpAddress(), 
            UserAgent(), 
            DeviceInfo());
            
        var result = await _sender.Send(command, cancellationToken);
        SetTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            dto.Email, 
            dto.Password, 
            dto.FullName, 
            dto.Role, 
            IpAddress(), 
            UserAgent(), 
            DeviceInfo());
            
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("refreshPolicy")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var token = request?.Token ?? Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(token))
            return BadRequest(new ProblemDetails { Title = "Token is required", Detail = "Refresh token must be provided in body or cookie." });

        var command = new RefreshTokenCommand(token, IpAddress(), UserAgent(), DeviceInfo());
        var result = await _sender.Send(command, cancellationToken);
        
        SetTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var token = request?.Token ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(token))
            return BadRequest(new ProblemDetails { Title = "Token is required" });

        var command = new RevokeTokenCommand(token, IpAddress());
        var result = await _sender.Send(command, cancellationToken);

        if (!result)
            return NotFound(new ProblemDetails { Title = "Token not found" });

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var command = new RevokeAllTokensCommand(userId, IpAddress());
        await _sender.Send(command, cancellationToken);

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    private void SetTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expiresAt,
            // Environment-based secure cookie policy
            Secure = !_env.IsDevelopment(), // In production, require HTTPS
            SameSite = _env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string? IpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }

    private string? UserAgent()
    {
        return Request.Headers["User-Agent"].ToString();
    }

    private string? DeviceInfo()
    {
        // Typically extracting device info from User-Agent or custom headers like X-Device-Id
        return Request.Headers.ContainsKey("X-Device-Id") 
            ? Request.Headers["X-Device-Id"].ToString() 
            : null;
    }
}

public class RefreshTokenRequest
{
    public string? Token { get; set; }
}

public class RevokeTokenRequest
{
    public string? Token { get; set; }
}
