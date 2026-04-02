using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Vktun.PhoneLogin;
using Vktun.Abp.Aliyun.Sms;

namespace Vktun.PhoneLogin;

public class PhoneLoginAppService : ApplicationService, IPhoneLoginAppService
{
    private readonly IPhoneNumberValidator _phoneNumberValidator;
    private readonly ISmsCodeStore _smsCodeStore;
    private readonly ISmsSender _smsSender;
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _userRepository;

    public PhoneLoginAppService(
        IPhoneNumberValidator phoneNumberValidator,
        ISmsCodeStore smsCodeStore,
        ISmsSender smsSender,
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository)
    {
        _phoneNumberValidator = phoneNumberValidator;
        _smsCodeStore = smsCodeStore;
        _smsSender = smsSender;
        _userManager = userManager;
        _userRepository = userRepository;
    }

    public async Task SendSmsCodeAsync(SendPhoneCodeInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);

        var templateCode = input.IsRegister
            ? await SettingProvider.GetOrNullAsync(PhoneLoginSettingNames.Sms.TemplateCodeForRegister)
            : await SettingProvider.GetOrNullAsync(PhoneLoginSettingNames.Sms.TemplateCode);

        var code = await _smsCodeStore.GenerateAndSetAsync(input.PhoneNumber);
        await _smsSender.SendAsync(new SmsSendRequest
        {
            PhoneNumber = input.PhoneNumber,
            SignName = (await SettingProvider.GetOrNullAsync(PhoneLoginSettingNames.Sms.SignName))!,
            TemplateCode = templateCode ?? "",
            TemplateParam = $"{{\"code\":\"{code}\"}}"
        });
    }

    public async Task<string> LoginAsync(PhoneLoginInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);
        await _smsCodeStore.ValidateAsync(input.PhoneNumber, input.Code);

        var user = await FindByPhoneNumberAsync(input.PhoneNumber);
        if (user == null)
            throw new UserFriendlyException(PhoneLoginErrorCodes.UserNotFoundByPhone, "User not found with this phone number");

        return user.Id.ToString();
    }

    public async Task RegisterAsync(PhoneRegisterInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);
        await _smsCodeStore.ValidateAsync(input.PhoneNumber, input.Code);

        var existingUser = await FindByPhoneNumberAsync(input.PhoneNumber);
        if (existingUser != null)
            throw new UserFriendlyException(PhoneLoginErrorCodes.PhoneAlreadyExists, "Phone number already registered");

        var tenantId = CurrentTenant.Id;
        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.UserName ?? input.PhoneNumber,
            tenantId.HasValue ? tenantId.Value.ToString() : null,
            CurrentTenant.Id
        );
        user.SetPhoneNumber(input.PhoneNumber, true);

        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException(result.Errors.Select(e => e.Description).JoinAsString(", "));
        }
    }

    public async Task ChangePasswordByPhoneAsync(ChangePasswordByPhoneInput input)
    {
        _phoneNumberValidator.Validate(input.PhoneNumber);
        await _smsCodeStore.ValidateAsync(input.PhoneNumber, input.Code);

        var user = await FindByPhoneNumberAsync(input.PhoneNumber);
        if (user == null)
            throw new UserFriendlyException(PhoneLoginErrorCodes.UserNotFoundByPhone, "User not found with this phone number");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, input.NewPassword);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException(result.Errors.Select(e => e.Description).JoinAsString(", "));
        }
    }

    private async Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber)
    {
        var users = await _userRepository.GetListAsync(skipCount: 0, maxResultCount: int.MaxValue, sorting: nameof(IdentityUser.Id));
        return users.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
    }
}
