using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

[DependsOn(
    typeof(PhoneLoginDomainModule),
    typeof(PhoneLoginApplicationContractsModule),
    typeof(AbpIdentityApplicationModule)
)]
public class PhoneLoginApplicationModule : AbpModule
{
}
