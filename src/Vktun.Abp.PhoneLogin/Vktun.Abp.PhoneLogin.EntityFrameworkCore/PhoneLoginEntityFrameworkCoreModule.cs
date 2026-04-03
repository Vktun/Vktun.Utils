using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Vktun.PhoneLogin.EntityFrameworkCore;

[DependsOn(
    typeof(PhoneLoginDomainModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class PhoneLoginEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IPhoneLoginUserLookup, EfCorePhoneLoginUserLookup>();
    }
}
