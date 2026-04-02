using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

[DependsOn(
    typeof(PhoneLoginApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpOpenIddictAspNetCoreModule)
)]
public class PhoneLoginHttpApiModule : AbpModule
{
}
