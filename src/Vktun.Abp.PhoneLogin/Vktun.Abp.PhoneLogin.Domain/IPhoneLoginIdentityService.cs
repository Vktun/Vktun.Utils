using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Identity;
using IdentityOptions = Microsoft.AspNetCore.Identity.IdentityOptions;
using IdentityResult = Microsoft.AspNetCore.Identity.IdentityResult;

namespace Vktun.PhoneLogin;

/// <summary>
/// Encapsulates identity operations required by phone login flows.
/// </summary>
public interface IPhoneLoginIdentityService
{
    Task CreateAsync(IdentityUser user, string password);

    Task AddDefaultRolesAsync(IdentityUser user);

    Task<string> GeneratePasswordResetTokenAsync(IdentityUser user);

    Task ResetPasswordAsync(IdentityUser user, string token, string newPassword);

    Task<bool> IsPhoneNumberConfirmedAsync(IdentityUser user);
}

/// <summary>
/// Default phone login identity service backed by <see cref="IdentityUserManager"/>.
/// </summary>
public class PhoneLoginIdentityService(
    IdentityUserManager userManager,
    IOptions<IdentityOptions> identityOptions) : IPhoneLoginIdentityService
{
    private readonly IdentityUserManager _userManager = userManager;
    private readonly IOptions<IdentityOptions> _identityOptions = identityOptions;

    public async Task CreateAsync(IdentityUser user, string password)
    {
        await _identityOptions.SetAsync();
        EnsureSucceeded(await _userManager.CreateAsync(user, password));
    }

    public async Task AddDefaultRolesAsync(IdentityUser user)
    {
        EnsureSucceeded(await _userManager.AddDefaultRolesAsync(user));
    }

    public Task<string> GeneratePasswordResetTokenAsync(IdentityUser user)
    {
        return _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task ResetPasswordAsync(IdentityUser user, string token, string newPassword)
    {
        EnsureSucceeded(await _userManager.ResetPasswordAsync(user, token, newPassword));
    }

    public Task<bool> IsPhoneNumberConfirmedAsync(IdentityUser user)
    {
        return _userManager.IsPhoneNumberConfirmedAsync(user);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new UserFriendlyException(result.Errors.Select(e => e.Description).JoinAsString(", "));
        }
    }
}
