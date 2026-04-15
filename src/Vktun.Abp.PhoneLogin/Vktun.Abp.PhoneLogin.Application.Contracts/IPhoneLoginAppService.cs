namespace Vktun.PhoneLogin;

public interface IPhoneLoginAppService
{
    Task SendSmsCodeAsync(SendPhoneCodeInput input);

    Task<string> LoginAsync(PhoneLoginInput input);

    Task<string> RequestTokenAsync(PhoneLoginInput input);

    Task<string> RequestTokenByCodeAsync(PhoneLoginInput input);

    Task<string> RequestTokenByPasswordAsync(PhonePasswordLoginInput input);

    Task RegisterAsync(PhoneRegisterInput input);

    Task ChangePasswordByPhoneAsync(ChangePasswordByPhoneInput input);
}
