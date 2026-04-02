using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin.Controllers;

[Route("api/phone-login")]
public class PhoneLoginController : AbpControllerBase
{
    private readonly IPhoneLoginAppService _phoneLoginAppService;

    public PhoneLoginController(IPhoneLoginAppService phoneLoginAppService)
    {
        _phoneLoginAppService = phoneLoginAppService;
    }

    [HttpPost("send-code")]
    public Task SendSmsCodeAsync([FromBody] SendPhoneCodeInput input)
        => _phoneLoginAppService.SendSmsCodeAsync(input);

    [HttpPost("login")]
    public Task<string> LoginAsync([FromBody] PhoneLoginInput input)
        => _phoneLoginAppService.LoginAsync(input);

    [HttpPost("register")]
    public Task RegisterAsync([FromBody] PhoneRegisterInput input)
        => _phoneLoginAppService.RegisterAsync(input);

    [HttpPost("change-password")]
    public Task ChangePasswordByPhoneAsync([FromBody] ChangePasswordByPhoneInput input)
        => _phoneLoginAppService.ChangePasswordByPhoneAsync(input);
}
