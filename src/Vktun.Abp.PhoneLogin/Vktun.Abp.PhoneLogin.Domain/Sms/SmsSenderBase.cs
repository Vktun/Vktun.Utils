using Microsoft.Extensions.Options;

namespace Vktun.PhoneLogin.Sms;

public abstract class SmsSenderBase : ISmsSender
{
    public abstract Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam);

    public async Task<bool> SendAsync(SmsSendRequest request)
    {
        return await SendAsync(request.PhoneNumber, "", request.TemplateCode, request.SignName, request.TemplateParam);
    }
}

public class AliyunSmsSender : SmsSenderBase
{
    private readonly AliyunSmsOptions _options;

    public AliyunSmsSender(IOptions<SmsProviderOptions> options)
    {
        _options = options.Value.Aliyun;
    }

    public override async Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam)
    {
        // 这里应该调用阿里云SMS SDK
        // 暂时返回模拟成功
        await Task.Delay(100);
        return true;
    }
}

public class TencentSmsSender : SmsSenderBase
{
    private readonly TencentSmsOptions _options;

    public TencentSmsSender(IOptions<SmsProviderOptions> options)
    {
        _options = options.Value.Tencent;
    }

    public override async Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam)
    {
        // 这里应该调用腾讯云SMS SDK
        // 暂时返回模拟成功
        await Task.Delay(100);
        return true;
    }
}
