using Volo.Abp.Settings;

namespace Vktun.PhoneLogin;

public class PhoneLoginSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(PhoneLoginSettingNames.Sms.SignName, string.Empty),
            new SettingDefinition(PhoneLoginSettingNames.Sms.TemplateCode, string.Empty),
            new SettingDefinition(PhoneLoginSettingNames.Sms.TemplateCodeForRegister, string.Empty),
            new SettingDefinition(PhoneLoginSettingNames.Sms.CodeLength, "6"),
            new SettingDefinition(PhoneLoginSettingNames.Sms.CodeExpireSeconds, "300"),
            new SettingDefinition(PhoneLoginSettingNames.Sms.MaxSendFrequencyInSeconds, "60"));
    }
}
