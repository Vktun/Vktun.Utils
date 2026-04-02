using Volo.Abp;
using Volo.Abp.Modularity;

namespace Vktun.PhoneLogin;

[DependsOn(typeof(AbpModule))]
public class PhoneLoginDomainSharedModule : AbpModule
{
}
