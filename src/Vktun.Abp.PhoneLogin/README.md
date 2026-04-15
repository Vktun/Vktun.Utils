# Vktun.Abp.PhoneLogin

ABP Framework 手机号登录模块，面向 NuGet 复用场景，提供短信验证码、手机号注册、手机号改密，以及 OpenIddict token 登录。

## 功能

- 手机号 + 验证码 token 登录
- 手机号 + 密码 token 登录
- 手机号注册并确认手机号
- 手机号验证码改密
- 默认短信码缓存实现，可替换 `ISmsCodeStore`
- 短信发送抽象 `ISmsSender`，内置 Aliyun/Tencent 发送器占位实现

## 模块引用

按宿主项目需要引用对应模块：

```csharp
[DependsOn(
    typeof(PhoneLoginApplicationModule),
    typeof(PhoneLoginHttpApiModule),
    typeof(PhoneLoginEntityFrameworkCoreModule)
)]
public class YourProjectModule : AbpModule
{
}
```

如果宿主项目已经有自己的用户查询实现，可以不引用 `PhoneLoginEntityFrameworkCoreModule`，自行注册 `IPhoneLoginUserLookup`。

## 配置

```json
{
  "AuthServer": {
    "Authority": "https://your-auth-server",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "Vktun": {
    "PhoneLogin": {
      "Sms": {
        "SignName": "your-sign-name",
        "TemplateCode": "login-template",
        "TemplateCodeForRegister": "register-template",
        "CodeLength": "6",
        "CodeExpireSeconds": "300",
        "ProviderName": "Aliyun"
      }
    }
  }
}
```

## OpenIddict

模块会注册自定义 grant handler，并允许 `phone_number_credentials` flow。客户端请求 `/connect/token` 时可使用以下两种凭据之一。

验证码登录：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=phone_number_credentials&
client_id=your-client-id&
client_secret=your-client-secret&
phonenumber=13800138000&
code=123456
```

密码登录：

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=phone_number_credentials&
client_id=your-client-id&
client_secret=your-client-secret&
phonenumber=13800138000&
password=YourPassword123!
```

## HTTP API

- `POST /api/phone-login/send-code`
- `POST /api/phone-login/register`
- `POST /api/phone-login/change-password`
- `POST /api/phone-login/request-token/by-code`
- `POST /api/phone-login/request-token/by-password`

兼容旧调用：

- `POST /api/phone-login/login` 等同于验证码 token 登录
- `POST /api/phone-login/request-token` 等同于验证码 token 登录

## 自定义短信发送

生产环境建议替换默认发送器：

```csharp
public class CustomSmsSender : ISmsSender
{
    public Task<bool> SendAsync(SmsSendRequest request)
    {
        // Call your SMS provider here.
        return Task.FromResult(true);
    }

    public Task<bool> SendAsync(
        string phoneNumber,
        string code,
        string templateCode,
        string signName,
        string templateParam)
    {
        return SendAsync(new SmsSendRequest
        {
            PhoneNumber = phoneNumber,
            TemplateCode = templateCode,
            SignName = signName,
            TemplateParam = templateParam
        });
    }
}
```

```csharp
context.Services.AddTransient<ISmsSender, CustomSmsSender>();
```
