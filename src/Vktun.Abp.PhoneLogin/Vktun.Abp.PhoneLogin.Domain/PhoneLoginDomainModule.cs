using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

[DependsOn(
    typeof(PhoneLoginDomainSharedModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpOpenIddictDomainModule)
)]
public class PhoneLoginDomainModule : AbpModule
{
}
