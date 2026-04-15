using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;

namespace Vktun.PhoneLogin;

public class PhonePasswordLoginInput
{
    [Required]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    [DisableAuditing]
    public string Password { get; set; } = null!;
}
