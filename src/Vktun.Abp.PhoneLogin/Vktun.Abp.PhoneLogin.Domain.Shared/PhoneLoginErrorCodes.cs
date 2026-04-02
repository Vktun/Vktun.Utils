namespace Vktun.PhoneLogin;

public static class PhoneLoginErrorCodes
{
    public const string InvalidPhoneNumber = "Vktun.PhoneLogin:010001";
    public const string SmsCodeSendFailed = "Vktun.PhoneLogin:010002";
    public const string SmsCodeExpired = "Vktun.PhoneLogin:010003";
    public const string SmsCodeInvalid = "Vktun.PhoneLogin:010004";
    public const string SmsCodeFrequentlySent = "Vktun.PhoneLogin:010005";
    public const string UserNotFoundByPhone = "Vktun.PhoneLogin:010006";
    public const string PhoneAlreadyExists = "Vktun.PhoneLogin:010007";
    public const string AliyunConfigMissing = "Vktun.PhoneLogin:020001";
}
