using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Vktun.Abp.Aliyun;

namespace Vktun.Abp.Aliyun.Sms;

[DependsOn(typeof(AbpAliyunBaseModule))]
public class AbpAliyunSmsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ISmsSender, AliyunSmsSender>();
    }
}
