using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Vktun.PhoneLogin.Validation;

public static class Program
{
    public static async Task<int> Main()
    {
        var port = GetAvailablePort();

        using var application = AbpApplicationFactory.Create<PhoneLoginValidationModule>();
        application.Initialize();

        var configuration = (IConfigurationRoot)application.ServiceProvider.GetRequiredService<IConfiguration>();
        configuration["AuthServer:Authority"] = $"http://127.0.0.1:{port}";

        var tokenServer = new ValidationTokenServer(application.ServiceProvider, port);
        tokenServer.Start();

        var appService = application.ServiceProvider.GetRequiredService<IPhoneLoginAppService>();
        var state = application.ServiceProvider.GetRequiredService<PhoneLoginValidationState>();

        const string phoneNumber = "13800138000";
        const string password = "Validation123!";

        try
        {
            await appService.SendSmsCodeAsync(new SendPhoneCodeInput
            {
                PhoneNumber = phoneNumber,
                IsRegister = true
            });

            await appService.RegisterAsync(new PhoneRegisterInput
            {
                PhoneNumber = phoneNumber,
                Code = state.LatestCodes[phoneNumber],
                Password = password,
                UserName = "validation-user"
            });

            await appService.SendSmsCodeAsync(new SendPhoneCodeInput
            {
                PhoneNumber = phoneNumber,
                IsRegister = false
            });

            var loginTokenJson = await appService.LoginAsync(new PhoneLoginInput
            {
                PhoneNumber = phoneNumber,
                Code = state.LatestCodes[phoneNumber]
            });

            EnsureAccessToken(loginTokenJson, nameof(IPhoneLoginAppService.LoginAsync));

            await appService.SendSmsCodeAsync(new SendPhoneCodeInput
            {
                PhoneNumber = phoneNumber,
                IsRegister = false
            });

            var requestTokenJson = await appService.RequestTokenAsync(new PhoneLoginInput
            {
                PhoneNumber = phoneNumber,
                Code = state.LatestCodes[phoneNumber]
            });

            EnsureAccessToken(requestTokenJson, nameof(IPhoneLoginAppService.RequestTokenAsync));

            var passwordTokenJson = await appService.RequestTokenByPasswordAsync(new PhonePasswordLoginInput
            {
                PhoneNumber = phoneNumber,
                Password = password
            });

            EnsureAccessToken(passwordTokenJson, nameof(IPhoneLoginAppService.RequestTokenByPasswordAsync));

            Console.WriteLine("Phone login validation passed.");
            Console.WriteLine(loginTokenJson);
            Console.WriteLine(requestTokenJson);
            Console.WriteLine(passwordTokenJson);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Phone login validation failed.");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            await tokenServer.DisposeAsync();
            application.Shutdown();
        }
    }

    private static void EnsureAccessToken(string tokenJson, string operationName)
    {
        using var json = JsonDocument.Parse(tokenJson);
        var accessToken = json.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException($"{operationName} did not return access_token.");
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
