using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Dysmsapi20170525;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using Microsoft.Extensions.Options;

namespace Vktun.Abp.Aliyun;

public interface IAliyunSmsService
{
    Task SendSmsAsync(string phoneNumber, string signName, string templateCode, string templateParam);
}

public class AliyunSmsService : IAliyunSmsService
{
    private readonly AbpAliyunOptions _options;
    private readonly Lazy<Client> _clientLazy;

    public AliyunSmsService(IOptions<AbpAliyunOptions> options)
    {
        _options = options.Value;
        _clientLazy = new Lazy<Client>(() =>
        {
            var config = new Config
            {
                AccessKeyId = _options.AccessKeyId,
                AccessKeySecret = _options.AccessKeySecret,
                Endpoint = _options.Endpoint
            };
            return new Client(config);
        });
    }

    public async Task SendSmsAsync(string phoneNumber, string signName, string templateCode, string templateParam)
    {
        var request = new SendSmsRequest
        {
            PhoneNumbers = phoneNumber,
            SignName = signName,
            TemplateCode = templateCode,
            TemplateParam = templateParam
        };

        var response = await _clientLazy.Value.SendSmsAsync(request);

        if (response.Body.Code != "OK")
        {
            throw new Exception($"Aliyun SMS send failed: Code={response.Body.Code}, Message={response.Body.Message}");
        }
    }
}
