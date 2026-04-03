using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vktun.PhoneLogin.Sms;

public interface ISmsProviderFactory
{
    ISmsSender CreateSender();
}

public class SmsProviderFactory : ISmsProviderFactory
{
    private readonly SmsProviderOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public SmsProviderFactory(IOptions<SmsProviderOptions> options, IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    public ISmsSender CreateSender()
    {
        return _options.ProviderName switch
        {
            "Aliyun" => _serviceProvider.GetRequiredService<AliyunSmsSender>(),
            "Tencent" => _serviceProvider.GetRequiredService<TencentSmsSender>(),
            _ => _serviceProvider.GetRequiredService<AliyunSmsSender>()
        };
    }
}
