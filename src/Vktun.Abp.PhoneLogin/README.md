# Vktun.Abp.PhoneLogin

基于 ABP Framework 的手机号登录模块，支持：

- 手机号验证码登录
- 手机号注册
- 手机号修改密码
- 多SMS厂商支持（阿里云、腾讯云）
- OpenIddict token 授权模式

## 快速开始

### 1. 安装依赖

```bash
dotnet add package Vktun.Abp.PhoneLogin
```

### 2. 模块配置

在 `YourProjectModule.cs` 中添加依赖：

```csharp
[DependsOn(
    typeof(VktunAbpPhoneLoginModule)
)]
```

### 3. 配置 SMS 厂商

在 `appsettings.json` 中配置 SMS 厂商：

```json
{
  "Vktun": {
    "PhoneLogin": {
      "Sms": {
        "ProviderName": "Aliyun", // 可选：Aliyun, Tencent
        "Aliyun": {
          "AccessKeyId": "your-aliyun-access-key",
          "AccessKeySecret": "your-aliyun-access-secret",
          "RegionId": "cn-hangzhou"
        },
        "Tencent": {
          "SecretId": "your-tencent-secret-id",
          "SecretKey": "your-tencent-secret-key",
          "Region": "ap-guangzhou",
          "SdkAppId": "your-tencent-sdk-app-id"
        }
      }
    }
  }
}
```

### 4. 配置 OpenIddict

在 `OpenIddictServerModule` 中添加手机号登录的授权类型：

```csharp
Configure<OpenIddictServerOptions>(options =>
{
    options.GrantTypes.Add(PhoneLoginConsts.GrantType);
});
```

## API 接口

### 发送验证码

**POST** `/api/phone-login/send-code`

```json
{
  "phoneNumber": "13800138000",
  "isRegister": false
}
```

### 登录（返回用户ID）

**POST** `/api/phone-login/login`

```json
{
  "phoneNumber": "13800138000",
  "code": "123456"
}
```

### 获取 Token

**POST** `/api/phone-login/request-token`

```json
{
  "phoneNumber": "13800138000",
  "code": "123456"
}
```

### 注册

**POST** `/api/phone-login/register`

```json
{
  "phoneNumber": "13800138000",
  "code": "123456",
  "password": "YourPassword123!",
  "userName": "optional-username"
}
```

### 修改密码

**POST** `/api/phone-login/change-password`

```json
{
  "phoneNumber": "13800138000",
  "code": "123456",
  "newPassword": "NewPassword123!"
}
```

## OpenIddict Token Endpoint

使用自定义授权类型 `phone_number_credentials` 获取 token：

**POST** `/connect/token`

```json
{
  "grant_type": "phone_number_credentials",
  "phonenumber": "13800138000",
  "code": "123456",
  "client_id": "your-client-id",
  "client_secret": "your-client-secret"
}
```

## 自定义 SMS 厂商

实现 `ISmsSender` 接口并注册到 DI 容器：

```csharp
public class CustomSmsSender : ISmsSender
{
    public async Task<bool> SendAsync(string phoneNumber, string code, string templateCode, string signName, string templateParam)
    {
        // 实现自定义 SMS 发送逻辑
        return true;
    }

    public async Task<bool> SendAsync(SmsSendRequest request)
    {
        return await SendAsync(request.PhoneNumber, "", request.TemplateCode, request.SignName, request.TemplateParam);
    }
}

// 在模块中注册
services.AddTransient<ISmsSender, CustomSmsSender>();
```
