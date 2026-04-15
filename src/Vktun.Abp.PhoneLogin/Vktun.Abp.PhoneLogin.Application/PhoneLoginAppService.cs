using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace Vktun.PhoneLogin;

/// <summary>
/// Provides phone-based registration, password reset, and token login flows.
/// </summary>
public class PhoneLoginAppService : ApplicationService, IPhoneLoginAppService
{
    private const string TenantHeaderName = "__tenant";

    private readonly IPhoneNumberValidator _phoneNumberValidator;
    private readonly ISmsCodeStore _smsCodeStore;
    private readonly ISmsSender _smsSender;
    private readonly IPhoneLoginIdentityService _identityService;
    private readonly IPhoneLoginUserLookup _userLookup;
    private readonly IConfiguration _configuration;
    private readonly ISettingProvider _settingProvider;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guidGenerator;

    public PhoneLoginAppService(
        IPhoneNumberValidator phoneNumberValidator,
        ISmsCodeStore smsCodeStore,
        ISmsSender smsSender,
        IPhoneLoginIdentityService identityService,
        IPhoneLoginUserLookup userLookup,
        IConfiguration configuration,
        ISettingProvider settingProvider,
        ICurrentTenant currentTenant,
        IGuidGenerator guidGenerator)
    {
        _phoneNumberValidator = phoneNumberValidator;
        _smsCodeStore = smsCodeStore;
        _smsSender = smsSender;
        _identityService = identityService;
        _userLookup = userLookup;
        _configuration = configuration;
        _settingProvider = settingProvider;
        _currentTenant = currentTenant;
        _guidGenerator = guidGenerator;
    }

    public async Task SendSmsCodeAsync(SendPhoneCodeInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);

        var templateCode = await GetRequiredSettingAsync(
            input.IsRegister
                ? PhoneLoginSettingNames.Sms.TemplateCodeForRegister
                : PhoneLoginSettingNames.Sms.TemplateCode);

        var codeLength = await GetSettingAsync(
            PhoneLoginSettingNames.Sms.CodeLength,
            PhoneLoginConsts.DefaultCodeLength);
        var expireSeconds = await GetSettingAsync(
            PhoneLoginSettingNames.Sms.CodeExpireSeconds,
            PhoneLoginConsts.DefaultCodeExpireSeconds);
        var code = await _smsCodeStore.GenerateAndSetAsync(input.PhoneNumber, codeLength, expireSeconds);
        var sendResult = await _smsSender.SendAsync(new SmsSendRequest
        {
            PhoneNumber = input.PhoneNumber,
            SignName = await GetRequiredSettingAsync(PhoneLoginSettingNames.Sms.SignName),
            TemplateCode = templateCode,
            TemplateParam = $"{{\"code\":\"{code}\"}}"
        });

        if (!sendResult)
        {
            throw new UserFriendlyException(
                PhoneLoginErrorCodes.SmsCodeSendFailed,
                "Failed to send SMS verification code");
        }
    }

    public async Task<string> LoginAsync(PhoneLoginInput input)
    {
        return await RequestTokenByCodeAsync(input);
    }

    public async Task RegisterAsync(PhoneRegisterInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);
        await _smsCodeStore.ValidateAsync(input.PhoneNumber, input.Code);

        var existingUser = await _userLookup.FindByPhoneNumberAsync(input.PhoneNumber);
        if (existingUser != null)
        {
            throw new UserFriendlyException(
                PhoneLoginErrorCodes.PhoneAlreadyExists,
                "Phone number already registered");
        }

        var user = new IdentityUser(
            _guidGenerator.Create(),
            input.UserName ?? $"Phone_{input.PhoneNumber}",
            $"{input.PhoneNumber}@phone.local",
            _currentTenant.Id);

        user.SetPhoneNumber(input.PhoneNumber, true);

        await _identityService.CreateAsync(user, input.Password);
        await _identityService.AddDefaultRolesAsync(user);
    }

    public async Task ChangePasswordByPhoneAsync(ChangePasswordByPhoneInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);
        await _smsCodeStore.ValidateAsync(input.PhoneNumber, input.Code);

        var user = await _userLookup.FindByPhoneNumberAsync(input.PhoneNumber);
        if (user == null)
        {
            throw new UserFriendlyException(
                PhoneLoginErrorCodes.UserNotFoundByPhone,
                "User not found with this phone number");
        }

        var token = await _identityService.GeneratePasswordResetTokenAsync(user);
        await _identityService.ResetPasswordAsync(user, token, input.NewPassword);
    }

    public async Task<string> RequestTokenAsync(PhoneLoginInput input)
    {
        return await RequestTokenByCodeAsync(input);
    }

    public async Task<string> RequestTokenByCodeAsync(PhoneLoginInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);

        return await RequestTokenInternalAsync(input.PhoneNumber, new Dictionary<string, string>
        {
            ["code"] = input.Code
        });
    }

    public async Task<string> RequestTokenByPasswordAsync(PhonePasswordLoginInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);

        return await RequestTokenInternalAsync(input.PhoneNumber, new Dictionary<string, string>
        {
            ["password"] = input.Password
        });
    }

    private async Task<string> RequestTokenInternalAsync(string phoneNumber, Dictionary<string, string> credentialParameters)
    {
        var authority = _configuration["AuthServer:Authority"]?.TrimEnd('/');
        var clientId = _configuration["AuthServer:ClientId"];
        var clientSecret = _configuration["AuthServer:ClientSecret"];

        if (authority.IsNullOrWhiteSpace() || clientId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(
                "Phone login token request requires AuthServer:Authority and AuthServer:ClientId configuration.");
        }

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = PhoneLoginConsts.GrantType,
            ["client_id"] = clientId!,
            ["phonenumber"] = phoneNumber
        };

        foreach (var parameter in credentialParameters)
        {
            form[parameter.Key] = parameter.Value;
        }

        if (!clientSecret.IsNullOrWhiteSpace())
        {
            form["client_secret"] = clientSecret!;
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{authority}/connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        if (_currentTenant.Id.HasValue)
        {
            request.Headers.Add(TenantHeaderName, _currentTenant.Id.Value.ToString());
        }

        using var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new UserFriendlyException(string.IsNullOrWhiteSpace(content)
                ? "Failed to request token from the OpenIddict token endpoint."
                : content);
        }
        return content;
    }

    private async Task<string> GetRequiredSettingAsync(string name)
    {
        var value = await _settingProvider.GetOrNullAsync(name)
            ?? _configuration[name.Replace('.', ':')];
        if (value.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException($"Missing required setting: {name}");
        }

        return value!;
    }

    private async Task<int> GetSettingAsync(string name, int defaultValue)
    {
        var value = await _settingProvider.GetOrNullAsync(name)
            ?? _configuration[name.Replace('.', ':')];

        return int.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }
}
