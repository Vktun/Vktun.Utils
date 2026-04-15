namespace Vktun.PhoneLogin;

public static class PhoneLoginConsts
{
    public const string GrantType = "phone_number_credentials";
    public const string VerificationCodeCachePrefix = "PhoneLogin:VerificationCode";
    public const string LoginPurposeName = "LoginByPhoneNumber";
    public const string ConfirmPurposeName = "ConfirmPhoneNumber";
    public const int DefaultCodeLength = 6;
    public const int DefaultCodeExpireSeconds = 300;
}
