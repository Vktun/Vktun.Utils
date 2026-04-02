namespace Vktun.PhoneLogin;

public interface ISmsCodeStore
{
    Task<string> GenerateAndSetAsync(string phoneNumber, int expireSeconds = 300);
    Task<bool> ValidateAsync(string phoneNumber, string code);
    Task RemoveAsync(string phoneNumber);
}
