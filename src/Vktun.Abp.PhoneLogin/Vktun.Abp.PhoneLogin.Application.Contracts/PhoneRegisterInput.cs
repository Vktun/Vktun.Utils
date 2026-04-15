using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;

namespace Vktun.PhoneLogin;

public class PhoneRegisterInput
{
    [Required]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    [DisableAuditing]
    public string Password { get; set; } = null!;

    [StringLength(256)]
    public string? UserName { get; set; }
}
