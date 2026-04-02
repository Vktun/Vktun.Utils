using System.ComponentModel.DataAnnotations;

namespace Vktun.PhoneLogin;

public class PhoneLoginInput
{
    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
}
