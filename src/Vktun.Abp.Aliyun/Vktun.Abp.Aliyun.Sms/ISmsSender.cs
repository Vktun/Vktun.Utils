namespace Vktun.Abp.Aliyun.Sms;

public interface ISmsSender
{
    Task SendAsync(SmsSendRequest request);
}
