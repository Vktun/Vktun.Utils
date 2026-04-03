using Volo.Abp.Identity;

namespace Vktun.PhoneLogin;

/// <summary>
/// Resolves users participating in the phone login flow.
/// </summary>
public interface IPhoneLoginUserLookup
{
    Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber);
}

/// <summary>
/// Default user lookup backed by ABP identity storage.
/// </summary>
public class IdentityPhoneLoginUserLookup(IIdentityUserRepository userRepository) : IPhoneLoginUserLookup
{
    private readonly IIdentityUserRepository _userRepository = userRepository;

    public async Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber)
    {
        var users = await _userRepository.GetListAsync();
        return users.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
    }
}
