namespace EcbatanLocation.Application.Services;

public record UserAccount(
    string UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles);

public record CreatedUserResult(string UserId, string GeneratedPassword);

public interface IUserService
{
    Task<IReadOnlyList<UserAccount>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserAccount?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<CreatedUserResult> CreateAsync(string displayName, string email, IReadOnlyList<string> roles, CancellationToken ct = default);
    Task UpdateAsync(string userId, string displayName, string email, IReadOnlyList<string> roles, CancellationToken ct = default);
    Task DeleteAsync(string userId, CancellationToken ct = default);
    Task<string> ResetPasswordAsync(string userId, CancellationToken ct = default);
    Task<int> CountUsersInRoleAsync(string role, CancellationToken ct = default);
}
