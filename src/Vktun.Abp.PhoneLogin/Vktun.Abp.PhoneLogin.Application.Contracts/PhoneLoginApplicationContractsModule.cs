using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

[DependsOn(
    typeof(PhoneLoginDomainSharedModule),
    typeof(AbpHttpClientModule)
)]
public class PhoneLoginApplicationContractsModule : AbpModule
{
}
