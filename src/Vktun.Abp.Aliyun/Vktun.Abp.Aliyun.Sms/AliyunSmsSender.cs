using Microsoft.Extensions.Logging;
using Volo.Abp;
using Vktun.Abp.Aliyun;

namespace Vktun.Abp.Aliyun.Sms;

public class AliyunSmsSender : ISmsSender
{
    private readonly IAliyunSmsService _aliyunSmsService;
    private readonly ILogger<AliyunSmsSender> _logger;

    public AliyunSmsSender(IAliyunSmsService aliyunSmsService, ILogger<AliyunSmsSender> logger)
    {
        _aliyunSmsService = aliyunSmsService;
        _logger = logger;
    }

    public async Task SendAsync(SmsSendRequest request)
    {
        try
        {
            await _aliyunSmsService.SendSmsAsync(
                request.PhoneNumber,
                request.SignName,
                request.TemplateCode,
                request.TemplateParam);
        }
        catch (Exception ex)
        {
            var message = $"Failed to send SMS verification code: {ex.Message}";
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", request.PhoneNumber);
            throw new UserFriendlyException(AliyunSmsErrorCodes.SmsSendFailed, message);
        }
    }
}

public static class AliyunSmsErrorCodes
{
    public const string SmsSendFailed = "Vktun.Abp.Aliyun.Sms:100001";
}
