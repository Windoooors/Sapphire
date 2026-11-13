# AGENTS.md

面向在本仓库中工作的 AI 编码代理的说明。开始改动前请先读完本文件；若本文件与代码不一致，以代码为准，并顺手修正本文件。

## 项目概览

Sapphire 是一个 QQ 机器人（群聊 + 私聊），通过 NapCat 的 WebSocket 接入 QQ（OneBot / go-cqhttp 协议，SDK 为 `EleCho.GoCqHttpSdk`）。主要功能：

- 舞萌 DX（maimai）：歌曲/别名检索、歌曲信息图、B50 图、成绩查询与更新、DivingFish 绑定与导出、华立 WCK 登录与成绩拉取、谱面容错查询。
- 中二节奏（Chunithm）：曲目数据（功能仍在搭建）。
- 群友：别名、搜索、娶群友、透群友、婚姻状态。
- 娱乐/工具：Ping、Gay 指数、计基助手（IEEE 754 浮点与整数进制转换）、按群/按人设置开关。

技术栈：.NET 10（`net10.0`）、C#（file-scoped namespace、nullable、collection expressions）、EF Core 10 + PostgreSQL（Npgsql）、Tomlyn（TOML 配置）、Serilog（控制台 + 按天滚动文件）、SixLabors.ImageSharp 4（绘图）。

仓库目前**没有测试项目**，正确性主要靠编译和手工联调保证。

## 常用命令

| 目的 | 命令 |
| --- | --- |
| 还原 + 编译 | `dotnet build Sapphire.sln` |
| 仅编译项目 | `dotnet build Sapphire/Sapphire.csproj` |
| 本地运行 | `dotnet run --project Sapphire/Sapphire.csproj` |
| Release 发布 | `dotnet publish Sapphire/Sapphire.csproj -c Release -o out` |
| Docker 构建 | `docker build -f Sapphire/Dockerfile -t sapphire .` |
| 新增 EF 迁移 | 见[数据库与迁移](#数据库与迁移) |

- 需要 .NET SDK 10（当前开发机为 10.0.403）；仓库没有 `global.json`。
- 编译产物在 `Sapphire/bin/<Configuration>/net10.0/`；`AppContext.BaseDirectory` 指向该目录，`config.toml` 与 `Resources/` 都相对于它。`dotnet run` 不会切换当前工作目录，Serilog 的 `logs/` 相对当前 shell 目录生成。
- 当前基线：`dotnet build Sapphire.sln` 为 0 warning / 0 error。提交前至少保证编译通过，不要新增警告。
- `dotnet run` 是常驻服务：会连接 PostgreSQL、加载曲目、反复重连 NapCat，不会自行退出。验证时请用后台任务或超时，不要在前台无限等待。

## 代码地图

```
Sapphire/
├── Program.cs                  # 入口：根指令注册、启动顺序、全局 HttpClient/Session/Timer
├── Sapphire.csproj             # net10.0，PackageReference 清单
├── Dockerfile
├── Commands/                   # 指令层
│   ├── CommandBase.cs          # 指令基类、CommandContext、FlagSpec、发消息/权限辅助
│   ├── MaiCommandBase.cs       # maimai 指令共用：歌曲匹配、列表文案
│   ├── GroupMemberCommandBase.cs
│   ├── ChuniCommandBase.cs
│   ├── MaiCommand.cs           # 功能域根指令，SubCommands 挂具体指令
│   ├── MaiCommands/            # 具体 maimai 指令
│   ├── GroupMemberCommands/    # 群友指令
│   └── ChuniCommands/
├── Data/                       # 数据访问与领域数据
│   ├── Mai/{Database,Dtos}     # 曲目元数据 + 用户成绩（bytea）
│   ├── DivingFishMai/          # DivingFish OAuth / 上传 / 拉取
│   ├── Wck/                    # 华立 WCK（成绩同步）
│   ├── GroupMember/            # 群友别名、婚姻
│   ├── Settings/               # 群/私聊设置
│   └── Chuni/
├── Environments/               # config.toml 模型与单例读取
├── ImageGeneration/            # ImageSharp 绘图：调色板、字体、图片生成器
├── Migrations/                 # EF Core 迁移（按 DbContext 分目录）
├── UserSettings/               # 设置项定义（标识符、显示名、默认值）
└── Utils/                      # 解析器、会话、日志、Timer、搜索归一化、进制转换
```

## 运行配置

配置来源是应用基目录下的 `config.toml`（`AppContext.BaseDirectory`），由 `Environments/SpEnvironmentHandler.cs` 在首次访问时自动生成默认文件，之后只读取。**配置写错导致反序列化失败时，整份文件会被默认配置覆盖写回**，修改前先备份。

节与关键字段：

- `[NapCat]`：`Ip`、`Port`、`Token`、`HeartBeatInterval`
- `[Database]`：`SqlConnectionString`（PostgreSQL）
- `[DivingFish]`：`ClientId`、`ClientSecret`、`DivingFishAuthUrl`、`DivingFishUrl`
- `[Mai]`：`CnLastestVersionIndexes`、`JpLastestVersionIndexes`
- `[WahlapConnectiveKits]`：`WckUrl`
- `[ImageGeneration]`：配色、`PreloadCoverImages`、`MaxParallelGenerationTaskCount`
- `[Sapphire]`：`BotName`、`AdministratorQqId`、`BotQqId`、`CommandPrefixes`、`UtcDayTickTime`

注意：

- `config.toml` 含 NapCat Token、数据库口令、DivingFish ClientSecret，**不要提交**，也不要在日志或回复中打印。`Sapphire/bin/.../config.toml` 只是本地产物（`bin/` 已被忽略）。
- 修改配置模型时同步修改 `Environments/SubEnvironments/*.cs`；字段名即 TOML 键名（Tomlyn 默认 PascalCase）。
- 启动顺序：初始化 Serilog → `new SettingsDataManager()`（迁移设置库）→ 各根指令 `StartInitialize()`（建库/迁移、加载曲目索引、预载封面）→ `BotSessionHandler.RunSession` 断线重连循环 → 主线程 `keepAlive.WaitOne()`。
- 缺少 PostgreSQL 或 NapCat 时：Debug 下初始化异常会直接抛出；Release 下记录错误后继续运行，但设置/指令不可用或反复重连。排查「指令没反应」先检查这两项。

## 指令系统

所有指令都继承 `CommandBase`。根指令在 `Program.cs` 的 `GroupCommands` 中注册；功能域指令（如 `MaiCommand`）把子指令放进 `SubCommands`，框架递归初始化与分发。

### 匹配规则（`Utils/CommandParser.cs`）

- 前缀形式：`<prefix> <head> [subhead...] [args]`。`prefix` 必须精确等于 `Sapphire.CommandPrefixes` 中的一项（**区分大小写**），默认 `s` / `sapphire` / `saf`；例如 `s mai search 恋`。
- 逐 token 沿 `SubCommands` 下钻，命中即停，剩余 token 作为参数。
- 简短形式（lite）：`lite_commands.enabled` 为 true 时，第一个 token 与任一 `DirectCommandHeads` 忽略大小写匹配，例如 `搜歌 恋`、`b50`、`娶群友`。
- 前缀形式优先于 lite 形式；两者都没命中则静默忽略。
- 快捷别名（`CommandParser.Shortcuts`）：`score` → `info -s`、`ap50` → `b50 -ap`，以及 `app50` / `fc50` / `fcp50` / `pc50` / `f50`，消息以该词开头即替换（不检查词边界）。
- 解析前会先让所有根指令执行 `ReceiveDirectly`；`ConfirmationCommand` 依赖它处理回复「确定/取消」，不要把它从 `GroupCommands` 中移除。
- 每个指令受 `ActivationSettingsIdentifier` 控制，按群或私聊用户从设置库读取；未配置时默认启用。

### 参数与 Flag

- `SupportedFlags` 中每个 `FlagSpec` 声明短名/长名、是否带值。`TakesValue = true` 时，值会一直收集到下一个 flag 为止。
- 处理器里用**长名**查询：`commandContext.GetFlag("--bpm")`、`commandContext.HasFlag("--divingfish")`。`FlagSpec.Name` 就是 `LongName`，所以**一定要设置 `LongName`**，否则会存成空键。
- 没有 flag 且没有位置参数时走 `Process`，否则走 `ProcessWithArguments`。`Argument` 是剩余 token 用空格拼接的字符串，可能为 `null`。
- 输出统一用 `SendMessage(context, [ ... ])`（默认引用回复）；`CommandBase` 提供 `SendArgumentError` / `SendUnknownError` / `SendUnpermittedError`，mai 域另有 `SendUnauthorizedError` / `SendNotLoggedInError`。
- 权限：`IsBotAdministrator` 比对 `Sapphire.AdministratorQqId`；`IsGroupAdministrator` 通过 NapCat API 查询群成员角色。

### 新增一个指令

1. 在 `Commands/<域>/` 新建类，继承 `CommandBase` 或对应域基类（`MaiCommandBase` 等）。
2. 构造函数里设置 `CommandHeads`、`DirectCommandHeads`、`SupportedFlags`、`ActivationSettingsIdentifier`。
3. 按需重写 `Process` / `ProcessWithArguments` / `ProcessDirectly` / `Initialize` / `OnExit`。
4. 注册：根指令加到 `Program.GroupCommands`，或在父指令的 `SubCommands` 数组里追加。
5. 若需要开关：在 `UserSettings/SettingsItem.cs` 的 `SettingsItems.Categories` 加显示项，并在 `DefaultValues` 加默认值。漏加 `DefaultValues` 时不会出现在 `settings` 列表，且默认按启用处理。
6. 运行 `dotnet build`，再按[验证与提交](#验证与提交)手工联调。

示例：

```csharp
using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class ExampleCommand : CommandBase
{
    public ExampleCommand()
    {
        CommandHeads = ["example"];
        DirectCommandHeads = ["例子"];
        ActivationSettingsIdentifier = "example.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                ShortName = "-v",
                LongName = "--verbose",
                TakesValue = false
            }
        ];
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        await SendMessage(context, ["用法：example <文本>"]);
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.Argument is null)
        {
            await SendArgumentError(context);
            return;
        }

        await SendMessage(context, [$"收到：{commandContext.Argument}"]);
    }
}
```

注册到根指令：

```csharp
private static readonly CommandBase[] GroupCommands =
[
    new PingCommand(),
    new ExampleCommand(), // 新增
    // ...
];
```

## 数据库与迁移

连接串统一来自 `SpEnvironmentHandler.Environment.Database.SqlConnectionString`，以下 DbContext 共用同一个 PostgreSQL 库：

| DbContext | 位置 | 迁移目录 | 说明 |
| --- | --- | --- | --- |
| `SettingsMetadataContext` | `Data/Settings/Database` | `Migrations/` | 群/私聊设置项 |
| `GroupMemberDataContext` | `Data/GroupMember/Database` | `Migrations/GroupMemberData/` | 群友、别名、婚姻 |
| `MaiMetadataContext` | `Data/Mai/Database` | 无 | 曲目/谱面/别名元数据，仓库外维护 |
| `MaiUserDataContext` | `Data/Mai/Database` | `Migrations/MaiUserData/` | 用户成绩 blob |
| `ChuniMetadataContext` | `Data/Chuni/Database` | 无 | 中二曲目元数据，仓库外维护 |

- 启动时各 Manager 构造函数调用 `Database.Migrate()`，正常部署不需要手工建表。
- `MaiMetadataContext` 与 `ChuniMetadataContext` 在仓库内**没有迁移**：曲目元数据由外部导入，`Migrate()` 不会创建 `MaiSongs` 等业务表。改这两个上下文前先确认真实库结构由谁维护，不要用迁移去「补建表」。
- 新增迁移（在仓库根执行；本机已全局安装 `dotnet-ef` 10.0.12，缺失时先 `dotnet tool install --global dotnet-ef`）：

  ```bash
  dotnet ef migrations add <Name> \
    --project Sapphire/Sapphire.csproj \
    --context GroupMemberDataContext \
    --output-dir Migrations/GroupMemberData
  ```

  其他上下文替换 `--context` / `--output-dir`：`SettingsMetadataContext` / `Migrations`，或 `MaiUserDataContext` / `Migrations/MaiUserData`。
- 生成的 `*.Designer.cs` 与 `*ModelSnapshot.cs` 不要手改；迁移类保持自动生成内容，必要时只补数据迁移逻辑。
- 写库用短生命周期 `await using var context = new XxxContext();`，不要跨请求复用；只读查询用 `AsNoTracking()`。
- `MaiUserPlayRecords.Data` 是 `bytea`，按 `MaiPlayRecord`（10 字节、little-endian）的原始内存布局存储。修改该 struct 的字段/顺序/大小会导致旧数据不可读，必须同时做数据迁移；`MaiPlayRecordBlobHelper` 有大小校验。
- 时间统一用 UTC（`DateTime.UtcNow`、`UtcDayTickTime`）；「自然日」边界由 `UtcDayTickTime` 决定。

## 图片生成

- `ImageGeneration/ColorPalette.cs` 静态构造时从 `AppContext.BaseDirectory/Resources/Fonts/` 读取 `light.otf`、`normal.otf`、`regular.otf`、`heavy.otf`、`fallback.ttf`、`emoji.ttf`；缺文件会在首次画图时抛异常。
- 封面：`Resources/Covers/<sapphireId>.png`、`Resources/CoversHd/<sapphireId>.png`，缺失时回退到 `0.png`。`PreloadCoverImages = true` 时启动即把 `Resources/Covers` 全部载入内存（`MaiImageGeneratorFunctions.LoadImages`）。
- 这些 `Resources/` 运行时资源**不在 git 中**，需要本地或部署环境自行提供；`dotnet build` 不会自动复制。
- 出图统一走 `ImageGenerationHandler.Instance.AddTask(...)`，由信号量按 `MaxParallelGenerationTaskCount` 限流；返回 base64 JPEG（质量 50），直接作为 CQ 图片消息发送。
- ImageSharp 对象实现 `IDisposable`，生成器里注意 `using`；`PingCommand` 会报告未释放的 Image 数量，可用于排查泄漏。
- `Sapphire/sixlabors.lic` 是 SixLabors 许可凭证（Community，2028-01-19 到期）；升级 ImageSharp 前确认许可要求。

## 编码约定

- 语言特性：file-scoped namespace；`ImplicitUsings` 与 `Nullable` 全开；优先使用 collection expressions（`[...]`）、`required` + `init`、record 承载配置/DTO。
- 格式：4 空格缩进，Allman 大括号；私有字段 `_camelCase`（`static readonly` 字段多为 PascalCase，跟随周边文件）；异步方法加 `Async` 后缀，返回 `Task` / `Task<T>`。
- 命名：类型/成员 PascalCase；指令类以 `Command` 结尾；设置标识符形如 `<域>_<功能>.enabled`（如 `mai_b50.enabled`）。
- 面向用户的文案用中文，代码标识符用英文；注释中英混用，跟随文件现状。
- Manager 使用 `public static XxxDataManager? Instance { get; private set; }` 单例，索引烘焙完成后再赋值；重建索引调用 `BakeIndexingData()`。访问 `Instance` 前判空，避免初始化失败时 NRE。
- 全局状态集中在 `Program`（`HttpClient`、`BotSessionHandler`、`Timer`）和各 Manager；新增共享可变状态必须加锁（参考 `MarryCommand` 的 `Lock`、`GroupMemberDataManager` 的索引重建）。
- 网络请求复用 `Program.HttpClient`，不要每次 `new HttpClient()`；HTTP/JSON 异常在边界捕获并用 Serilog 记录，Debug 下按现有模式 `throw`。
- 不在消息或日志中泄露令牌、连接串、ClientSecret。

## 验证与提交

- 当前没有自动化测试。改动后至少执行 `dotnet build Sapphire.sln`，保持 0 warning / 0 error。
- 涉及指令、数据库、图片的改动需要 NapCat + PostgreSQL + 本地 `Resources/` 才能端到端验证。无法联调时在回复中说明未验证项，不要声称「已测试」。
- 不要提交：`bin/`、`obj/`、`config.toml`、`logs/`、`.idea/`、`*.DotSettings.user`、本地生成的 `Resources/`（如需入库先与维护者确认）。
- 提交信息沿用历史风格：`feat:` / `fix:` + 英文简述（冒号后空一格，句首大写），尽量一次提交聚焦一件事；可参考 `git log --oneline`。
- 改动跨指令/数据层时，确认没有破坏 `Program.GroupCommands` 的注册和设置项默认值。
- 修改项目结构、命令注册方式、配置字段或构建命令后，同步更新本文件。

## 易错点

- `CommandBase.Receive` 的分支依据是「有没有 flag / 有没有位置参数」；只重写 `Process` 会导致带参数时落到基类默认回复 `dame`。
- `CommandHeads` 与 `DirectCommandHeads` 是两套；只配一个时另一种调用形式不可用。
- 前缀匹配区分大小写，lite 匹配不区分；`s` 与 `S` 不等价。
- 设置项按「群」或「私聊用户」存储；私聊里改的设置不影响群。
- `SettingsDataManager` 构造时把整张设置表读进内存，`SetValue` 会同时更新内存和数据库；绕过它直接改库不会即时生效。
- 主循环之外不要 `Thread.Sleep` 阻塞指令处理；耗时操作走异步或 `ImageGenerationHandler` 队列。
- `MaiDataManager` 的索引是常驻内存的；曲目或别名变化后需要重新烘焙（`BakeIndexingData`）或重启进程。
- `GroupMemberDataManager.GetMarriages()` 会先删除当天之前的记录，依赖 `UtcDayTickTime`；改动婚姻逻辑时注意这个副作用。

## 参考

- 远程仓库：<https://github.com/Windoooors/Sapphire>
