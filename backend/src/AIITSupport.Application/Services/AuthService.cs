using AIITSupport.Application.DTOs;
using AIITSupport.Application.Interfaces;
using AIITSupport.Domain.Entities;
using AIITSupport.Domain.Enums;
using AIITSupport.Domain.Interfaces;

namespace AIITSupport.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository users, ITokenService tokenService)
    {
        _users = users;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _users.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        // Every new self-registered user starts as a plain Employee.
        // Admin/SupportAgent roles should be assigned later by an Admin, not at signup.
        await _users.AddAsync(user, RoleNames.Employee);
        await _users.SaveChangesAsync();

        var roles = await _users.GetRoleNamesAsync(user.Id);
        var token = _tokenService.GenerateToken(user, roles);
        return new AuthResponse(token, user.Name, user.Email, roles);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = await _users.GetRoleNamesAsync(user.Id);
        var token = _tokenService.GenerateToken(user, roles);
        return new AuthResponse(token, user.Name, user.Email, roles);
    }
}
