namespace Vktun.PhoneLogin;

public interface ISmsSender
{
    Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam);
    Task<bool> SendAsync(SmsSendRequest request);
}

public class SmsSendRequest
{
    public string PhoneNumber { get; set; } = null!;
    public string SignName { get; set; } = null!;
    public string TemplateCode { get; set; } = null!;
    public string TemplateParam { get; set; } = null!;
}
