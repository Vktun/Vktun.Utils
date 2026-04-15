# Excel 核心处理引擎 NuGet 化整理规划

## 1. 目标

把当前仓库中分散在 `shared/Vktun.Shared.Common`、`shared/Vktun.Shared.Infrastructure`、`shared/Vktun.Shared.Hosting` 和部分业务模块中的 Excel 通用能力整理为可复用 NuGet 包，供 Reporting、DyhyCharge、MeterCollection、后续业务模块统一使用。

核心目标：

- 抽出通用 Excel 导入、导出、模板渲染、字段描述、工作流编排能力。
- 业务模块只依赖稳定抽象和少量定义类，不直接依赖 `MiniExcel`、`ClosedXML`、`Magicodes`。
- NuGet 包不反向依赖当前业务模块、ABP 应用层、EF Core、HttpApi 或具体数据库。
- ASP.NET 文件上传/下载、ABP DI 注册、业务模板解析器作为适配层，不放进纯核心引擎。

## 2. 当前代码分布

| 位置 | 当前职责 | NuGet 化建议 |
|---|---|---|
| `shared/Vktun.Shared.Common/Excel/ExcelContracts.cs` | 导出请求、Sheet、Column、导入导出服务接口、模板解析接口、请求 Builder | 迁入核心 Abstractions 包 |
| `shared/Vktun.Shared.Common/Excel/ExcelWorkflowContracts.cs` | `ExcelColumnAttribute`、文件 DTO、模板描述、导入导出定义、Orchestrator 接口、字段反射工厂 | 迁入核心 Abstractions 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/DefaultExcelExportService.cs` | MiniExcel / ClosedXML / Magicodes 导出、模板导出、多 Sheet、分 Sheet、格式化 | 迁入 Engine 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/DefaultExcelImportService.cs` | ClosedXML 导入、模板生成、类型转换、行错误收集 | 迁入 Engine 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/DefaultExcelWorkflowServices.cs` | 模板描述、导出编排、导入预览和提交编排 | 迁入 Engine 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/ControlledExcelImportFileResolver.cs` | 受控文件路径解析和导入流打开 | 迁入 Engine 包或 FileSystem 子包 |
| `shared/Vktun.Shared.Infrastructure/Excel/ExcelImportCompatibilityOptions.cs` | 文件导入兼容配置 | 迁入 Engine 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/DefaultExcelImportCompatibilityService.cs` | 基于文件路径的兼容导入 | 迁入 Engine 包 |
| `shared/Vktun.Shared.Infrastructure/Excel/LegacyExcelCompatibility.cs` | 静态兼容入口 | 迁入 Legacy 子包或保留在当前仓库过渡 |
| `shared/Vktun.Shared.Hosting/Excel/ExcelHttpResultFactory.cs` | ASP.NET `FileContentResult` 和 `IFormFile` 读取 | 放入 AspNetCore 适配包，不放核心包 |
| `modules/reporting/.../ReportingExcelTemplateResolver.cs` | Reporting 数据库模板解析 | 保留在 Reporting 模块，作为业务模板源适配器 |
| `services/dyhycharge/.../ExcelInfrastructureTests.cs` | 现有核心能力测试 | 迁移或复制到 NuGet 包测试项目 |
| `oldTempCode` 中 Magicodes DTO / Filter | 老系统兼容样例 | 不直接迁入；只作为兼容需求参考 |

## 3. 推荐包结构

建议先做 3 个包，避免一个包同时拖入 ASP.NET、ABP 和业务依赖。

```text
src/
  Vktun.Excel.Abstractions/
  Vktun.Excel.Engine/
  Vktun.Excel.AspNetCore/
tests/
  Vktun.Excel.Engine.Tests/
```

### 3.1 `Vktun.Excel.Abstractions`

纯抽象和模型包，不依赖 ASP.NET、ABP、EF Core、业务模块。

建议包含：

- `ExcelEngineHint`
- `ExcelExportRequest`
- `ExcelSheetDefinition`
- `ExcelColumnDefinition`
- `ExcelTypedColumnDefinition<T>`
- `ExcelExportRequestBuilder`
- `IExcelExportProfile<T>`
- `IExcelExportService`
- `IExcelImportService`
- `IExcelImportCompatibilityService`
- `IExcelImportFileResolver`
- `IExcelTemplateResolver`
- `ExcelImportResult<T>`
- `ExcelContentTypes`
- `ExcelTemplateDirection`
- `ExcelColumnAttribute`
- `ExcelFileDto`
- `ExcelImportFileInput`
- `ExcelColumnDescriptorDto`
- `ExcelTemplateDescriptorDto`
- `ExcelRowErrorDto`
- `ExcelImportPreviewDto<TRow>`
- `IExcelExportDefinition<TQuery, TRow>`
- `IExcelImportDefinition<TRow>`
- `IExcelImportCommitter<TRow, TResult>`
- `IExcelTemplateService`
- `IExcelExportOrchestrator`
- `IExcelImportOrchestrator`
- `ExcelColumnDefinitionFactory`

依赖建议：

```xml
<TargetFramework>net8.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

如果当前平台统一使用 `net10.0`，包可以先多目标：

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

不建议只做 `net10.0`，否则 NuGet 复用范围会被预览 SDK 限制。

### 3.2 `Vktun.Excel.Engine`

默认 Excel 引擎实现包。依赖 `Vktun.Excel.Abstractions`，并封装第三方 Excel 库。

建议包含：

- `DefaultExcelExportService`
- `DefaultExcelImportService`
- `DefaultExcelTemplateService`
- `DefaultExcelExportOrchestrator`
- `DefaultExcelImportOrchestrator`
- `ControlledExcelImportFileResolver`
- `DefaultExcelImportCompatibilityService`
- `ExcelImportCompatibilityOptions`
- `ServiceCollection` 注册扩展，例如 `AddVktunExcelEngine(...)`

当前第三方依赖：

| 依赖 | 当前版本 | 当前用途 |
|---|---:|---|
| `MiniExcel` | `1.43.0` | 普通导出、多 Sheet、模板渲染 |
| `ClosedXML` | `0.105.0` | 导入、模板生成、隐藏列导出 |
| `Magicodes.ExporterAndImporter.Core` | `1.0.1` | 兼容旧 DTO Header Attribute 读取 |
| `Magicodes.ExporterAndImporter.Excel` | `1.0.1` | 旧导出兼容和部分简单对象导出 |

建议第一阶段保留三套引擎，保证现有行为不变。第二阶段再考虑把 Magicodes 标记为 legacy。

### 3.3 `Vktun.Excel.AspNetCore`

ASP.NET 适配包。依赖 `Vktun.Excel.Abstractions` 和 `Microsoft.AspNetCore.Mvc`。

建议包含：

- `ExcelHttpResultFactory.File(ExcelFileDto file)`
- `ExcelHttpResultFactory.ReadAsync(IFormFile file, string? templateCode, CancellationToken cancellationToken)`

该包不依赖 `Vktun.Excel.Engine`，避免只需要 HTTP DTO 转换的项目被迫安装 Excel 引擎实现。

## 4. 不建议放入 NuGet 核心包的内容

以下内容应留在业务模块或当前解决方案中：

| 内容 | 原因 |
|---|---|
| `ReportingExcelTemplateResolver` | 依赖 Reporting 数据库、租户上下文和仓储，是业务模板源适配器 |
| 报表导出上限、定时任务、产物保留 | 属于 Reporting 业务规则 |
| ABP `ApplicationService`、仓储、权限 | 属于应用层，不是 Excel 核心能力 |
| `ExcelHttpResultFactory` | ASP.NET 适配，放 `Vktun.Excel.AspNetCore` |
| `VktunInfrastructureExtensions` 中 Redis、RabbitMQ、SnowId 注册 | 与 Excel 无关，不能带入 Excel NuGet |
| `oldTempCode` 业务 DTO 和 Magicodes Filter | 旧业务兼容样例，不是通用引擎 |

## 5. 建议命名空间

当前命名空间是：

```text
Vktun.Shared.Common.Excel
Vktun.Shared.Infrastructure.Excel
Vktun.Shared.Hosting.Excel
```

NuGet 化后建议统一为：

```text
Vktun.Excel
Vktun.Excel.Abstractions
Vktun.Excel.Engine
Vktun.Excel.AspNetCore
```

迁移时可以保留旧命名空间的兼容类型转发或薄包装，降低一次性改动：

```text
Vktun.Shared.Common.Excel -> type forwarding / obsolete wrapper
Vktun.Shared.Infrastructure.Excel -> obsolete wrapper
```

如果短期不做类型转发，则需要全仓替换 using 和项目引用。

## 6. 推荐公共 API 形态

### 6.1 导出

面向动态表格：

```csharp
var request = new ExcelExportRequest
{
    FileName = "report.xlsx",
    MaxRowsPerSheet = 50000,
    Sheets =
    [
        new ExcelSheetDefinition
        {
            Name = "report",
            Columns = columns,
            Rows = rows
        }
    ]
};

var bytes = await excelExportService.ExportAsync(request, cancellationToken);
```

面向强类型 DTO：

```csharp
var file = await excelExportOrchestrator.ExportAsync(definition, query, cancellationToken);
```

### 6.2 导入

生成模板：

```csharp
var file = await excelTemplateService.GenerateImportTemplateAsync(definition, cancellationToken);
```

预览导入：

```csharp
var preview = await excelImportOrchestrator.PreviewAsync(definition, stream, cancellationToken);
```

提交导入：

```csharp
var result = await excelImportOrchestrator.CommitAsync(committer, preview.Rows, cancellationToken);
```

### 6.3 模板渲染

当前模板渲染通过：

```csharp
ExcelExportRequest.TemplateCode
ExcelExportRequest.TemplatePath
ExcelExportRequest.TemplateValues
IExcelTemplateResolver.ResolvePathAsync(...)
```

第一阶段可保留 `TemplatePath` 模式。第二阶段建议新增 Stream 模式，避免数据库模板必须落临时文件：

```csharp
public interface IExcelTemplateResolver
{
    Task<ExcelTemplateContent?> ResolveAsync(string templateCode, CancellationToken cancellationToken = default);
}

public sealed class ExcelTemplateContent
{
    public string FileName { get; init; } = "template.xlsx";
    public Stream Content { get; init; } = Stream.Null;
}
```

但这会改动 `DefaultExcelExportService` 的模板渲染路径。建议作为 v2，不放第一阶段。

## 7. 迁移步骤

### 阶段 1：机械搬迁，保持行为不变

1. 新建独立仓库或目录，例如 `packages/excel/`。
2. 创建 `Vktun.Excel.Abstractions`。
3. 从 `shared/Vktun.Shared.Common/Excel` 复制 contracts、attributes、DTO、builder、factory。
4. 创建 `Vktun.Excel.Engine`。
5. 从 `shared/Vktun.Shared.Infrastructure/Excel` 复制默认服务实现和 Options。
6. 创建 `Vktun.Excel.AspNetCore`。
7. 从 `shared/Vktun.Shared.Hosting/Excel` 复制 HTTP helper。
8. 迁移 `ExcelInfrastructureTests` 到 `Vktun.Excel.Engine.Tests`。
9. 保持所有公开类型和方法签名不变，先不做架构重写。

### 阶段 2：发布内部预览包

1. 为包增加 NuGet 元数据：`PackageId`、`VersionPrefix`、`Authors`、`RepositoryUrl`、`Description`。
2. 固定第三方包版本。
3. 执行：

```powershell
dotnet pack src/Vktun.Excel.Abstractions/Vktun.Excel.Abstractions.csproj -c Release
dotnet pack src/Vktun.Excel.Engine/Vktun.Excel.Engine.csproj -c Release
dotnet pack src/Vktun.Excel.AspNetCore/Vktun.Excel.AspNetCore.csproj -c Release
```

4. 推送到内部 NuGet 源。
5. 在当前仓库引入预览包并替换项目引用。

### 阶段 3：当前仓库替换

1. `shared/Vktun.Shared.Common` 移除或保留 obsolete wrapper。
2. `shared/Vktun.Shared.Infrastructure` 移除 Excel 第三方包引用。
3. `shared/Vktun.Shared.Infrastructure/VktunInfrastructureExtensions.cs` 改成调用 `services.AddVktunExcelEngine(configuration)`。
4. `shared/Vktun.Shared.Hosting` 改为引用 `Vktun.Excel.AspNetCore`。
5. Reporting、DyhyCharge、MeterCollection 等模块使用 NuGet 包中的抽象。
6. 跑现有构建和 Excel 测试。

### 阶段 4：能力增强

建议后续增强：

- 支持模板 Stream 渲染，减少数据库模板落临时文件。
- 导入支持多 Sheet。
- 导入支持列别名、枚举映射、字典映射。
- 导出支持数据类型写入，而不是全部转字符串。
- 支持样式策略、冻结首行、列宽策略。
- 支持 CSV 引擎，适合大数据量导出。
- 支持大文件流式导出，避免全部载入内存。

## 8. 测试计划

从现有 `services/dyhycharge/Vktun.DyhyChargeService.Tests/ExcelInfrastructureTests.cs` 迁移并扩展：

| 测试 | 覆盖点 |
|---|---|
| 多 Sheet 导出 | `ExcelExportRequest.Sheets` 和 Sheet 名唯一化 |
| 分 Sheet 导出 | `MaxRowsPerSheet` |
| 隐藏列导出 | ClosedXML 引擎选择 |
| 模板导出 | `TemplateCode` / `TemplatePath` / `TemplateValues` |
| 导入模板生成 | `ExcelColumnAttribute`、必填、示例值 |
| 导入解析 | string、decimal、Guid、DateTime、DateOnly、bool、enum |
| 业务校验合并 | `IExcelImportDefinition.ValidateAsync` |
| 文件路径安全 | `ControlledExcelImportFileResolver` allowed roots |
| ASP.NET helper | `ExcelHttpResultFactory` 文件名和 ContentType |

## 9. 主要风险

| 风险 | 处理建议 |
|---|---|
| 当前代码依赖 `Vktun.Shared.Common.Excel` 命名空间 | 第一版可保留旧命名空间，包名先变；第二版再统一命名空间 |
| Magicodes 版本老，和新运行时兼容性未知 | 保留兼容测试；新能力优先走 MiniExcel / ClosedXML |
| 模板渲染目前依赖文件路径 | 第一阶段继续使用路径 resolver；第二阶段改 Stream resolver |
| 大数据量导出占用内存 | 给 Reporting 继续保留行数和文件大小上限；后续做 CSV/streaming |
| DTO 中存在业务中文 Header 编码问题 | NuGet 包只处理字符串；业务侧负责资源文件和编码质量 |
| ABP 注册和纯 .NET 注册混杂 | NuGet 包只提供 `IServiceCollection` 扩展，不依赖 ABP Module |

## 10. 最终建议

第一版 NuGet 不要过度拆引擎插件。建议先发布：

```text
Vktun.Excel.Abstractions
Vktun.Excel.Engine
Vktun.Excel.AspNetCore
```

其中 `Vktun.Excel.Engine` 继续内置 MiniExcel、ClosedXML、Magicodes 三套实现，保证当前行为稳定。等 Reporting 和业务模块完成替换后，再把 Magicodes 逐步降级为 legacy adapter，最后根据实际用量决定是否拆成 `Vktun.Excel.Magicodes`。
