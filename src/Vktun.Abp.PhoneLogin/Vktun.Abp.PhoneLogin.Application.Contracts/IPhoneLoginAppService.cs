namespace Vktun.PhoneLogin;

public interface IPhoneLoginAppService
{
    Task SendSmsCodeAsync(SendPhoneCodeInput input);
    Task<string> LoginAsync(PhoneLoginInput input);
    Task RegisterAsync(PhoneRegisterInput input);
    Task ChangePasswordByPhoneAsync(ChangePasswordByPhoneInput input);
}
