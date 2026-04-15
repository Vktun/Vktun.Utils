using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;

namespace Vktun.PhoneLogin.OpenIddict;

/// <summary>
/// Handles the custom <c>phone_number_credentials</c> grant.
/// </summary>
public class PhoneNumberGrantHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    public static readonly OpenIddictServerHandlerDescriptor Descriptor =
        OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.HandleTokenRequestContext>()
            .UseScopedHandler<PhoneNumberGrantHandler>()
            .SetOrder(100_000)
            .Build();

    private readonly IPhoneNumberValidator _phoneNumberValidator;
    private readonly ISmsCodeStore _smsCodeStore;
    private readonly IPhoneLoginUserLookup _userLookup;
    private readonly IPhoneLoginIdentityService _identityService;

    public PhoneNumberGrantHandler(
        IPhoneNumberValidator phoneNumberValidator,
        ISmsCodeStore smsCodeStore,
        IPhoneLoginUserLookup userLookup,
        IPhoneLoginIdentityService identityService)
    {
        _phoneNumberValidator = phoneNumberValidator;
        _smsCodeStore = smsCodeStore;
        _userLookup = userLookup;
        _identityService = identityService;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!string.Equals(context.Request.GrantType, PhoneLoginConsts.GrantType, StringComparison.Ordinal))
        {
            return;
        }

        var phoneNumber = context.Request.GetParameter("phone_number")?.ToString()
            ?? context.Request.GetParameter("phonenumber")?.ToString();
        var code = context.Request.GetParameter("code")?.ToString();
        var password = context.Request.GetParameter("password")?.ToString();

        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(password)))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidRequest,
                description: "Phone number and either code or password are required");
            return;
        }

        try
        {
            _phoneNumberValidator.Validate(phoneNumber);

            var user = await _userLookup.FindByPhoneNumberAsync(phoneNumber);
            if (user == null)
            {
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidGrant,
                    description: "User not found");
                return;
            }

            if (!await _identityService.IsPhoneNumberConfirmedAsync(user))
            {
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidGrant,
                    description: "Phone number not confirmed");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await _smsCodeStore.ValidateAsync(phoneNumber, code!);
            }
            else if (!await _identityService.CheckPasswordAsync(user, password))
            {
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidGrant,
                    description: "Invalid password");
                return;
            }

            var principal = CreateClaimsPrincipal(user, context);
            context.SignIn(principal);
        }
        catch (UserFriendlyException ex)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidGrant,
                description: ex.Message);
        }
        catch (Exception)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.ServerError,
                description: "An error occurred during authentication");
        }
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(
        IdentityUser user,
        OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString()));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName ?? string.Empty));
        identity.AddClaim(new Claim(AbpClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(AbpClaimTypes.UserName, user.UserName ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            identity.AddClaim(new Claim("phonenumber", user.PhoneNumber));
        }

        if (user.TenantId.HasValue)
        {
            identity.AddClaim(new Claim(AbpClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(context.Request.GetScopes());
        principal.SetResources(context.Request.GetResources());
        principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());

        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
