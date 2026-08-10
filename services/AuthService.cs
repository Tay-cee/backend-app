using backend_app.Configuration;
using backend_app.Data;
using backend_app.Exceptions;
using backend_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace backend_app.services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest req, string? ip);
    Task<AuthResponse> LoginAsync(LoginRequest req, string? ip);
    Task<AuthResponse> RefreshAsync(RefreshRequest req, string? ip);
    Task RevokeAsync(string refreshToken, string? ip);
}

public class AuthService : IAuthService
{
    private readonly JsonDataStore _store;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _jwt;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthService(JsonDataStore store, ITokenService tokens, IOptions<JwtOptions> jwt)
    {
        _store = store; _tokens = tokens; _jwt = jwt.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, string? ip)
    {
        if (_store.Users.Any(u => u.Email == req.Email))
            throw new ApiException(StatusCodes.Status409Conflict, "Email already registered.");
        if (_store.Users.Any(u => u.UserName == req.UserName))
            throw new ApiException(StatusCodes.Status409Conflict, "Username already taken.");

        // The very first account in a fresh deployment is seeded as Admin so that
        // Admin-only endpoints (e.g. deleting employees) are reachable at all.
        var isFirstUser = !_store.Users.Any();

        var user = new AppUser { Email = req.Email, UserName = req.UserName };
        if (isFirstUser)
            user.Roles = new List<string> { "User", "Admin" };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _store.AddUser(user);
        await _store.SaveChangesAsync();

        return await IssueTokensAsync(user, ip);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, string? ip)
    {
        var user = _store.Users.FirstOrDefault(u => u.Email == req.Email);

        // Prevent timing attacks by running hash verification even if user is null
        var dummyUser = new AppUser();
        var hashToVerify = user?.PasswordHash ?? dummyUser.PasswordHash;
        var result = _hasher.VerifyHashedPassword(dummyUser, hashToVerify, req.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid credentials.");

        return await IssueTokensAsync(user, ip);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, string? ip)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new ApiException(StatusCodes.Status400BadRequest, "Refresh token missing.");

        var incomingHash = _tokens.HashToken(req.RefreshToken);

        // The client's access token has usually expired by the time it refreshes (that's the point),
        // so it's only used, when present, as a fast path to find the user. Falling back to scanning
        // by refresh-token hash lets a client refresh from the HttpOnly cookie alone (e.g. right after
        // a page load, before it has any access token in memory).
        AppUser? user = null;
        if (!string.IsNullOrWhiteSpace(req.AccessToken))
        {
            var principal = _tokens.GetPrincipalFromExpiredToken(req.AccessToken);
            if (principal is not null && Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                user = _store.Users.FirstOrDefault(u => u.Id == userId);
        }
        user ??= _store.Users.FirstOrDefault(u => u.RefreshTokens.Any(rt => rt.TokenHash == incomingHash));

        if (user is null)
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token not recognized.");

        var existing = user.RefreshTokens.FirstOrDefault(rt => rt.TokenHash == incomingHash);

        if (existing is null)
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token not recognized.");

        if (!existing.IsActive)
        {
            if (existing.IsRevoked)
            {
                // Reuse detection: revoke entire family
                await RevokeDescendantsAsync(existing, user, ip, "Reuse detected");
                await _store.SaveChangesAsync();
            }
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token is no longer valid.");
        }

        // Rotate token
        var (newToken, newHash) = _tokens.GenerateRefreshToken();
        var replacement = new RefreshToken
        {
            TokenHash = newHash,
            ParentTokenHash = existing.TokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays)
        };

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReasonRevoked = "Rotated";
        existing.ReplacedByTokenHash = newHash;

        user.RefreshTokens.Add(replacement);
        await _store.SaveChangesAsync();

        var accessToken = _tokens.GenerateAccessToken(user);
        return new AuthResponse(
            accessToken, newToken,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            replacement.ExpiresAt,
            ToUserDto(user));
    }

    public async Task RevokeAsync(string refreshToken, string? ip)
    {
        var hash = _tokens.HashToken(refreshToken);
        var token = _store.Users.SelectMany(u => u.RefreshTokens).FirstOrDefault(rt => rt.TokenHash == hash);
        if (token is null || !token.IsActive) return;

        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "Manual revoke";
        await _store.SaveChangesAsync();
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, string? ip)
    {
        var accessToken = _tokens.GenerateAccessToken(user);
        var (refresh, refreshHash) = _tokens.GenerateRefreshToken();

        var rt = new RefreshToken
        {
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
        };

        user.RefreshTokens.Add(rt);
        await _store.SaveChangesAsync();

        return new AuthResponse(
            accessToken, refresh,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            rt.ExpiresAt,
            ToUserDto(user));
    }

    private async Task RevokeDescendantsAsync(RefreshToken token, AppUser user, string? ip, string reason)
    {
        var stack = new Stack<RefreshToken>();
        stack.Push(token);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current.IsRevoked) continue;
            current.RevokedAt = DateTime.UtcNow;
            current.ReasonRevoked = reason;

            var children = user.RefreshTokens
                .Where(rt => rt.ParentTokenHash == current.TokenHash && !rt.IsRevoked);
            foreach (var child in children) stack.Push(child);
        }
        await Task.CompletedTask;
    }

    private static UserDto ToUserDto(AppUser u) =>
        new(u.Id, u.UserName, u.Email, u.Roles);
}
