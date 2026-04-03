using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Settings;
using Vktun.PhoneLogin.OpenIddict;
using Vktun.PhoneLogin.Sms;

namespace Vktun.PhoneLogin;

[DependsOn(
    typeof(PhoneLoginDomainSharedModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpSettingsModule)
)]
public class PhoneLoginDomainModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.AllowCustomFlow(PhoneLoginConsts.GrantType);
            builder.AddEventHandler(PhoneNumberGrantHandler.Descriptor);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<SmsProviderOptions>(configuration.GetSection("Vktun:PhoneLogin:Sms"));

        context.Services.AddTransient<IPhoneNumberValidator, PhoneNumberValidator>();
        context.Services.AddTransient<IPhoneLoginIdentityService, PhoneLoginIdentityService>();
        context.Services.AddTransient<IPhoneLoginUserLookup, IdentityPhoneLoginUserLookup>();
        context.Services.AddTransient<ISmsCodeStore, DefaultSmsCodeStore>();
        context.Services.AddSingleton<ISmsProviderFactory, SmsProviderFactory>();
        context.Services.AddTransient<AliyunSmsSender>();
        context.Services.AddTransient<TencentSmsSender>();
        context.Services.AddTransient<ISmsSender>(sp => sp.GetRequiredService<ISmsProviderFactory>().CreateSender());
        context.Services.AddScoped<PhoneNumberGrantHandler>();
    }
}
