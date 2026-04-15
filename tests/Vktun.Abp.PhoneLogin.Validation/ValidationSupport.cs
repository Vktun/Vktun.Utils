using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Identity;

namespace Vktun.PhoneLogin.Validation;

public class PhoneLoginValidationState
{
    public ConcurrentDictionary<string, IdentityUser> Users { get; } = new();

    public ConcurrentDictionary<string, string> Passwords { get; } = new();

    public ConcurrentDictionary<string, string> LatestCodes { get; } = new();
}

public class ValidationSmsSender(PhoneLoginValidationState state) : ISmsSender
{
    private readonly PhoneLoginValidationState _state = state;

    public Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam)
    {
        using var payload = JsonDocument.Parse(templateParam);
        _state.LatestCodes[phoneNumber] = payload.RootElement.GetProperty("code").GetString()!;
        return Task.FromResult(true);
    }

    public Task<bool> SendAsync(SmsSendRequest request)
    {
        return SendAsync(request.PhoneNumber, string.Empty, request.TemplateCode, request.SignName, request.TemplateParam);
    }
}

public class InMemoryPhoneLoginUserLookup(PhoneLoginValidationState state) : IPhoneLoginUserLookup
{
    private readonly PhoneLoginValidationState _state = state;

    public Task<IdentityUser?> FindByPhoneNumberAsync(string phoneNumber)
    {
        _state.Users.TryGetValue(phoneNumber, out var user);
        return Task.FromResult(user);
    }
}

public class InMemoryPhoneLoginIdentityService(PhoneLoginValidationState state) : IPhoneLoginIdentityService
{
    private readonly PhoneLoginValidationState _state = state;

    public Task CreateAsync(IdentityUser user, string password)
    {
        _state.Users[user.PhoneNumber!] = user;
        _state.Passwords[user.PhoneNumber!] = password;
        return Task.CompletedTask;
    }

    public Task AddDefaultRolesAsync(IdentityUser user)
    {
        return Task.CompletedTask;
    }

    public Task<string> GeneratePasswordResetTokenAsync(IdentityUser user)
    {
        return Task.FromResult($"reset::{user.Id:N}");
    }

    public Task ResetPasswordAsync(IdentityUser user, string token, string newPassword)
    {
        if (token != $"reset::{user.Id:N}")
        {
            throw new InvalidOperationException("Unexpected reset token.");
        }

        _state.Passwords[user.PhoneNumber!] = newPassword;
        return Task.CompletedTask;
    }

    public Task<bool> IsPhoneNumberConfirmedAsync(IdentityUser user)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task<bool> CheckPasswordAsync(IdentityUser user, string password)
    {
        return Task.FromResult(
            user.PhoneNumber is not null &&
            _state.Passwords.TryGetValue(user.PhoneNumber, out var currentPassword) &&
            currentPassword == password);
    }
}

public sealed class ValidationTokenServer(IServiceProvider serviceProvider, int port) : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly HttpListener _listener = new();
    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private bool _started;

    public string Authority => $"http://127.0.0.1:{port}";

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Prefixes.Add($"{Authority}/");
        _listener.Start();
        _started = true;
        _processingTask = Task.Run(() => ProcessAsync(_cts.Token));
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        if (_started)
        {
            try
            {
                _listener.Stop();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (HttpListenerException)
            {
            }
        }

        if (_processingTask is not null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        try
        {
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
        }

        _cts.Dispose();
        _cts = null;
        _started = false;
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            await HandleAsync(context);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "POST" || context.Request.Url?.AbsolutePath != "/connect/token")
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Close();
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IPhoneNumberValidator>();
        var codeStore = scope.ServiceProvider.GetRequiredService<ISmsCodeStore>();
        var userLookup = scope.ServiceProvider.GetRequiredService<IPhoneLoginUserLookup>();
        var identityService = scope.ServiceProvider.GetRequiredService<IPhoneLoginIdentityService>();

        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var form = QueryHelpers.ParseQuery(body);

        var grantType = form.TryGetValue("grant_type", out var grantTypeValue) ? grantTypeValue.ToString() : string.Empty;
        var phoneNumber = form.TryGetValue("phonenumber", out var phoneNumberValue) ? phoneNumberValue.ToString() : string.Empty;
        var code = form.TryGetValue("code", out var codeValue) ? codeValue.ToString() : string.Empty;
        var password = form.TryGetValue("password", out var passwordValue) ? passwordValue.ToString() : string.Empty;

        if (grantType != PhoneLoginConsts.GrantType)
        {
            await WriteJsonAsync(context, HttpStatusCode.BadRequest, new { error = "unsupported_grant_type" });
            return;
        }

        try
        {
            validator.Validate(phoneNumber);

            var user = await userLookup.FindByPhoneNumberAsync(phoneNumber);
            if (user is null || !await identityService.IsPhoneNumberConfirmedAsync(user))
            {
                await WriteJsonAsync(context, HttpStatusCode.BadRequest, new { error = "invalid_grant" });
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await codeStore.ValidateAsync(phoneNumber, code);
            }
            else if (!await identityService.CheckPasswordAsync(user, password))
            {
                await WriteJsonAsync(context, HttpStatusCode.BadRequest, new { error = "invalid_grant" });
                return;
            }

            await WriteJsonAsync(context, HttpStatusCode.OK, new
            {
                access_token = $"phone-login-{user.Id:N}",
                token_type = "Bearer",
                expires_in = 3600,
                user_id = user.Id
            });
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(context, HttpStatusCode.BadRequest, new
            {
                error = "invalid_grant",
                error_description = ex.Message
            });
        }
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, HttpStatusCode statusCode, object payload)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(context.Response.OutputStream, payload);
        context.Response.Close();
    }
}
