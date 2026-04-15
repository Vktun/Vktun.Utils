namespace Vktun.PhoneLogin;

public interface ISmsCodeStore
{
    Task<string> GenerateAndSetAsync(
        string phoneNumber,
        int expireSeconds = PhoneLoginConsts.DefaultCodeExpireSeconds);

    Task<string> GenerateAndSetAsync(
        string phoneNumber,
        int codeLength,
        int expireSeconds)
    {
        return GenerateAndSetAsync(phoneNumber, expireSeconds);
    }

    Task<bool> ValidateAsync(string phoneNumber, string code);

    Task RemoveAsync(string phoneNumber);
}
