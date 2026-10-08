using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;                                    // added
using AuthApi.Settings;                                    // added
using Microsoft.AspNetCore.Authorization;                  // added
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;                        // added
using Npgsql;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing");

    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;          // added
    private readonly JwtSettings _jwt;                     // added

    public AuthController(
        AppDbContext db,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _tokenService = tokenService;
        _jwt = jwtOptions.Value;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(SignupRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PassWordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        var hashToCheck = user?.PassWordHash ?? DummyHash;
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, hashToCheck);

        if (user is null || !user.IsActive || !passwordOk)
            return Unauthorized(new { message = "Invalid email or password." });

        // Changed: return a signed JWT instead of only the user details
        var accessToken = _tokenService.CreateAccessToken(user);

        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,                 // JavaScript cannot read it (blocks XSS theft)
            Secure = true,                   // sent only over HTTPS
            SameSite = SameSiteMode.Lax,     // not sent on cross-site POSTs (CSRF protection)
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes)
        });

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresInSeconds = _jwt.AccessTokenMinutes * 60,
            User = ToResponse(user)
        });
    }

    // Added: a protected endpoint. Without a valid Bearer token this returns 401.
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirst("sub")?.Value,
            email = User.FindFirst("email")?.Value,
            fullName = User.FindFirst("name")?.Value,
            role = User.FindFirst("role")?.Value,
            tokenId = User.FindFirst("jti")?.Value
        });
    }

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role
    };
}