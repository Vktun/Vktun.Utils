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

    public Task<string> GenerateAndSetAsync(
        string phoneNumber,
        int expireSeconds = PhoneLoginConsts.DefaultCodeExpireSeconds)
    {
        return GenerateAndSetAsync(phoneNumber, PhoneLoginConsts.DefaultCodeLength, expireSeconds);
    }

    public async Task<string> GenerateAndSetAsync(
        string phoneNumber,
        int codeLength = PhoneLoginConsts.DefaultCodeLength,
        int expireSeconds = PhoneLoginConsts.DefaultCodeExpireSeconds)
    {
        var code = GenerateNumericCode(codeLength);
        var key = GetCacheKey(phoneNumber);
        await _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expireSeconds)
        });

        return code;
    }

    public async Task<bool> ValidateAsync(string phoneNumber, string code)
    {
        var key = GetCacheKey(phoneNumber);
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
        await _cache.RemoveAsync(GetCacheKey(phoneNumber));
    }

    private static string GetCacheKey(string phoneNumber)
    {
        return $"{PhoneLoginConsts.VerificationCodeCachePrefix}:{phoneNumber}";
    }

    private static string GenerateNumericCode(int codeLength)
    {
        if (codeLength < 4 || codeLength > 9)
        {
            codeLength = PhoneLoginConsts.DefaultCodeLength;
        }

        var min = (int)Math.Pow(10, codeLength - 1);
        var max = (int)Math.Pow(10, codeLength) - 1;
        return RandomNumberGenerator.GetInt32(min, max + 1).ToString();
    }
}
