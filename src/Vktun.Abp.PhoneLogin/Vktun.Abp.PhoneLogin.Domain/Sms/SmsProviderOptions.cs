namespace Vktun.PhoneLogin.Sms;

public class SmsProviderOptions
{
    public string ProviderName { get; set; } = "Aliyun";
    public AliyunSmsOptions Aliyun { get; set; } = new AliyunSmsOptions();
    public TencentSmsOptions Tencent { get; set; } = new TencentSmsOptions();
}

public class AliyunSmsOptions
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string RegionId { get; set; } = "cn-hangzhou";
}

public class TencentSmsOptions
{
    public string SecretId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-guangzhou";
    public string SdkAppId { get; set; } = string.Empty;
}
