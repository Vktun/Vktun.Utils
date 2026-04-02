using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using Volo.Abp;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

public class DefaultSmsCodeStore : ISmsCodeStore
{
    private readonly IDistributedCache _cache;

    public DefaultSmsCodeStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateAndSetAsync(string phoneNumber, int expireSeconds = 300)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var key = $"SmsCode:{phoneNumber}";
        await _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expireSeconds)
        });
        return code;
    }

    public async Task<bool> ValidateAsync(string phoneNumber, string code)
    {
        var key = $"SmsCode:{phoneNumber}";
        var cachedCode = await _cache.GetStringAsync(key);
        if (cachedCode == null || cachedCode != code)
        {
            throw new UserFriendlyException(PhoneLoginErrorCodes.SmsCodeInvalid, "Invalid or expired SMS verification code");
        }
        await _cache.RemoveAsync(key);
        return true;
    }

    public async Task RemoveAsync(string phoneNumber)
    {
        var key = $"SmsCode:{phoneNumber}";
        await _cache.RemoveAsync(key);
    }
}
