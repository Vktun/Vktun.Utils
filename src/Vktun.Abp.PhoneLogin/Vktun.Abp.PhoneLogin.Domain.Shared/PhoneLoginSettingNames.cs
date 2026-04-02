namespace Vktun.PhoneLogin;

public static class PhoneLoginSettingNames
{
    public const string SectionName = "Vktun.PhoneLogin";

    public static class Sms
    {
        public const string SignName = SectionName + ":Sms:SignName";
        public const string TemplateCode = SectionName + ":Sms:TemplateCode";
        public const string TemplateCodeForRegister = SectionName + ":Sms:TemplateCodeForRegister";
        public const string CodeLength = SectionName + ":Sms:CodeLength";
        public const string CodeExpireSeconds = SectionName + ":Sms:CodeExpireSeconds";
        public const string MaxSendFrequencyInSeconds = SectionName + ":Sms:MaxSendFrequencyInSeconds";
    }
}
