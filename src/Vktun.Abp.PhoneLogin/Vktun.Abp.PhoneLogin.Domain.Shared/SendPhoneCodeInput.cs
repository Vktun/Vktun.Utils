using System.ComponentModel.DataAnnotations;

namespace Vktun.PhoneLogin;

public class SendPhoneCodeInput
{
    [Required]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = null!;

    public bool IsRegister { get; set; }
}
