using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Infrastructure.Identity;

namespace EcbatanLocation.Infrastructure.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IUserService
{
    public async Task<IReadOnlyList<UserAccount>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = userManager.Users.ToList();
        var result = new List<UserAccount>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserAccount(user.Id, user.DisplayName, user.Email!, roles.ToList()));
        }

        return result.OrderBy(u => u.DisplayName).ToList();
    }

    public async Task<UserAccount?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserAccount(user.Id, user.DisplayName, user.Email!, roles.ToList());
    }

    public async Task<CreatedUserResult> CreateAsync(
        string displayName, string email, IReadOnlyList<string> roles, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException($"Un utilisateur avec l'email '{email}' existe déjà.");

        var password = GeneratePassword();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                await userManager.AddToRoleAsync(user, role);
        }

        return new CreatedUserResult(user.Id, password);
    }

    public async Task UpdateAsync(
        string userId, string displayName, string email, IReadOnlyList<string> roles, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
                throw new InvalidOperationException($"Un utilisateur avec l'email '{email}' existe déjà.");
        }

        user.DisplayName = displayName;
        user.Email = email;
        user.UserName = email;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", updateResult.Errors.Select(e => e.Description)));

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(roles).ToList();
        var rolesToAdd = roles.Except(currentRoles).ToList();

        if (rolesToRemove.Count > 0)
            await userManager.RemoveFromRolesAsync(user, rolesToRemove);

        foreach (var role in rolesToAdd)
        {
            if (await roleManager.RoleExistsAsync(role))
                await userManager.AddToRoleAsync(user, role);
        }
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException("Utilisateur introuvable.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<string> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException("Utilisateur introuvable.");

        var password = GeneratePassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(e => e.Description)));

        return password;
    }

    public async Task<int> CountUsersInRoleAsync(string role, CancellationToken ct = default)
    {
        var usersInRole = await userManager.GetUsersInRoleAsync(role);
        return usersInRole.Count;
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;

        Span<char> password = stackalloc char[12];
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        for (int i = 3; i < 12; i++)
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        var array = password.ToArray();
        RandomNumberGenerator.Shuffle<char>(array);
        return new string(array);
    }
}
