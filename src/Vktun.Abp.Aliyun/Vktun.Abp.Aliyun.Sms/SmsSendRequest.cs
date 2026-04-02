namespace Vktun.Abp.Aliyun.Sms;

public class SmsSendRequest
{
    public string PhoneNumber { get; set; } = null!;
    public string SignName { get; set; } = null!;
    public string TemplateCode { get; set; } = null!;
    public string TemplateParam { get; set; } = "{}";
}
