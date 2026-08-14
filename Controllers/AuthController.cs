using backend_app.Models;
using backend_app.DTOs.User;
using backend_app.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_app.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;
    public AuthController(IAuthService auth, IWebHostEnvironment env)
    {
        _auth = auth;
        _env = env;
    }

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var res = await _auth.RegisterAsync(req, Ip);
        SetRefreshCookie(res.RefreshToken, res.RefreshTokenExpiresAt);
        return Ok(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await _auth.LoginAsync(req, Ip);
        SetRefreshCookie(res.RefreshToken, res.RefreshTokenExpiresAt);
        return Ok(res);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body)
    {
        // Refresh token can come from HttpOnly cookie or body
        var refreshToken = body.RefreshToken ?? Request.Cookies["refresh_token"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(new { error = "Refresh token missing." });

        var req = body with { RefreshToken = refreshToken };
        var res = await _auth.RefreshAsync(req, Ip);
        SetRefreshCookie(res.RefreshToken, res.RefreshTokenExpiresAt);
        return Ok(res);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refresh_token", out var rt) && !string.IsNullOrEmpty(rt))
            await _auth.RevokeAsync(rt, Ip);
        Response.Cookies.Delete("refresh_token");
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
        return Ok(new { id, userName, email, roles });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok("You are admin.");

    private void SetRefreshCookie(string token, DateTime expires)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // dev server (Vite) talks to the API over plain http on localhost:5188
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            IsEssential = true,
            Path = "/api/auth"
        });
    }
}
