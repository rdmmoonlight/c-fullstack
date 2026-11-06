using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AumoFinance.Services;

/// <summary>Identitas pengguna dari sesi login (cookie Identity milik backend).</summary>
public sealed class UserContext
{
    private readonly AuthenticationStateProvider authState;

    public UserContext(AuthenticationStateProvider authState) => this.authState = authState;

    public async Task<ClaimsPrincipal> GetPrincipalAsync() => (await authState.GetAuthenticationStateAsync()).User;

    /// <summary>Guid.Empty bila belum login.</summary>
    public async Task<Guid> GetUserIdAsync()
    {
        var user = await GetPrincipalAsync();
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
