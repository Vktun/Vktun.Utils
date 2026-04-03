using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace Vktun.PhoneLogin.Validation;

[DependsOn(typeof(PhoneLoginApplicationModule))]
public class PhoneLoginValidationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthServer:Authority"] = "http://127.0.0.1:39215",
                ["AuthServer:ClientId"] = "phone-login-validation-client",
                ["AuthServer:ClientSecret"] = "phone-login-validation-secret",
                ["Vktun:PhoneLogin:Sms:SignName"] = "ValidationSign",
                ["Vktun:PhoneLogin:Sms:TemplateCode"] = "ValidationLoginTemplate",
                ["Vktun:PhoneLogin:Sms:TemplateCodeForRegister"] = "ValidationRegisterTemplate"
            })
            .Build();

        context.Services.AddDistributedMemoryCache();
        context.Services.AddSingleton<IConfiguration>(configuration);
        context.Services.AddSingleton<PhoneLoginValidationState>();

        context.Services.RemoveAll<ISmsSender>();
        context.Services.RemoveAll<IPhoneLoginIdentityService>();
        context.Services.RemoveAll<IPhoneLoginUserLookup>();

        context.Services.AddSingleton<ISmsSender, ValidationSmsSender>();
        context.Services.AddSingleton<IPhoneLoginIdentityService, InMemoryPhoneLoginIdentityService>();
        context.Services.AddSingleton<IPhoneLoginUserLookup, InMemoryPhoneLoginUserLookup>();
    }
}
