using System.Text.RegularExpressions;
using Volo.Abp;
using Vktun.PhoneLogin;

namespace Vktun.PhoneLogin;

public class PhoneNumberValidator : IPhoneNumberValidator
{
    private static readonly Regex PhoneRegex = new(@"^1[3-9]\d{9}$", RegexOptions.Compiled);

    public bool Validate(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new UserFriendlyException(PhoneLoginErrorCodes.InvalidPhoneNumber, "Phone number cannot be empty");

        if (!PhoneRegex.IsMatch(phoneNumber))
            throw new UserFriendlyException(PhoneLoginErrorCodes.InvalidPhoneNumber, "Invalid phone number format");

        return true;
    }
}
