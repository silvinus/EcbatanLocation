using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EcbatanLocation.Application.Tests;

public sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _user = CreateOwner();

    public void SetOwner() => _user = CreateOwner();
    public void SetAdmin() => _user = CreateAdmin();
    public void SetAnonymous() => _user = new ClaimsPrincipal(new ClaimsIdentity());

    /// <summary>
    /// Impersonates a specific seeded owner: the <paramref name="userId"/> matches the Identity user
    /// linked to an <c>Owner</c> record, so ownership checks resolve to that owner.
    /// </summary>
    public void SetOwner(string userId, string name)
        => _user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, "Owner"),
        ], "Test"));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_user));

    private static ClaimsPrincipal CreateOwner()
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "test-owner"),
            new Claim(ClaimTypes.Role, "Owner"),
        ], "Test"));

    private static ClaimsPrincipal CreateAdmin()
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "test-admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Owner"),
        ], "Test"));
}
