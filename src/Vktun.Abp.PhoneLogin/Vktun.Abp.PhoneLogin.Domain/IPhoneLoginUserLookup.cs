using Volo.Abp.Identity;

namespace Vktun.PhoneLogin;

/// <summary>
/// Resolves users participating in the phone login flow.
/// </summary>
public interface IPhoneLoginUserLookup
{
    Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber);
}
