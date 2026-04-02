using System.ComponentModel.DataAnnotations;

namespace Vktun.PhoneLogin;

public class ChangePasswordByPhoneInput
{
    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string NewPassword { get; set; } = null!;
}
