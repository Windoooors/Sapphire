# AGENTS.md

面向在本仓库中工作的 AI 编码代理的说明。开始改动前请先读完本文件；若本文件与代码不一致，以代码为准，并顺手修正本文件。

## 项目概览

Sapphire 是一个 QQ 机器人（群聊 + 单聊），通过 **官方 QQ 机器人开放平台** 的 WebSocket 网关接入 QQ，SDK 为 `QQBot.Net`（本地打补丁的 fork，见下）。主要功能：

- 舞萌 DX（maimai）：歌曲/别名检索、歌曲信息图、B50 图、成绩查询与更新、DivingFish 绑定与导出、华立 WCK 登录与成绩拉取、谱面容错查询。
- 中二节奏（Chunithm）：曲目数据（功能仍在搭建）。
- 群友：别名、搜索、娶群友、透群友、婚姻状态。
- 娱乐/工具：Ping、WhoAmI、Gay 指数、计基助手（IEEE 754 浮点与整数进制转换）、按群/按人设置开关。
- 旧库迁移：群聊用 `migrate <QQ号> --group <旧群号>` 迁移成绩、别名、群成员和设置；私聊用 `migrate <QQ号>` 只迁移成绩和私聊设置。由独立的 `Sapphire.Migration` 项目完成，主 bot 在回调后自动重建索引。

技术栈：.NET 10（`net10.0`）、C#（file-scoped namespace、nullable、collection expressions）、EF Core 10 + PostgreSQL（Npgsql）、Tomlyn（TOML 配置）、Serilog（控制台 + 按天滚动文件）、SixLabors.ImageSharp 4（绘图）、QQBot.Net（官方 QQ Bot API）。

仓库目前**没有测试项目**，正确性主要靠编译和手工联调保证。

> **依赖打补丁的 SDK**：`Sapphire/Sapphire.csproj` 通过 git submodule 引用 `third_party/QQBot.Net`（fork：<https://github.com/Windoooors/QQBot.Net>）。clone 仓库后先执行 `git submodule update --init --recursive`；默认 `QQBotNetRoot=$(MSBuildThisFileDirectory)..\third_party\QQBot.Net`，可用 `-p:QQBotNetRoot=...` 指向别的 checkout。这个 fork 在 `SocketMessage` 上暴露了官方网关下发、但 QQBot.Net 0.2.0 未解析的 `AuthorUsername` / `AuthorGroupNickname` / `AuthorMemberRole` / `AuthorUnionOpenId` / `AuthorAvatar`。Docker 构建同样要求 submodule 已初始化。

## 常用命令

| 目的 | 命令 |
| --- | --- |
| 初始化 submodule | `git submodule update --init --recursive` |
| 还原 + 编译 | `dotnet build Sapphire.sln` |
| 仅编译项目 | `dotnet build Sapphire/Sapphire.csproj` |
| 本地运行 | `dotnet run --project Sapphire/Sapphire.csproj` |
| Release 发布 | `dotnet publish Sapphire/Sapphire.csproj -c Release -o out` |
| 运行迁移服务 | 复制 `Sapphire.Migration/config.example.toml` 到输出目录 `config.toml` 后 `dotnet run --project Sapphire.Migration/Sapphire.Migration.csproj` |
| Docker 构建 | `git submodule update --init --recursive && docker build -f Sapphire/Dockerfile -t sapphire .` |
| 新增 EF 迁移 | 见[数据库与迁移](#数据库与迁移) |

- 需要 .NET SDK 10（当前开发机为 10.0.403）；仓库没有 `global.json`。
- QQBot.Net 位于 `third_party/QQBot.Net`（git submodule，fork 只改了 `src/QQBot.Net.WebSocket` 的事件字段解析）。不要直接改 submodule 里的代码；补丁应提交到 fork 后更新 submodule 指针。
- 编译产物在 `Sapphire/bin/<Configuration>/net10.0/`；`AppContext.BaseDirectory` 指向该目录，`config.toml` 与 `Resources/` 都相对于它。`dotnet run` 不会切换当前工作目录，Serilog 的 `logs/` 相对当前 shell 目录生成。
- 当前基线：`dotnet build Sapphire.sln` 为 0 warning / 0 error。提交前至少保证编译通过，不要新增警告。
- `dotnet run` 是常驻服务：会连接 PostgreSQL、加载曲目、连接 QQ 网关，不会自行退出。验证时请用后台任务或超时，不要在前台无限等待。
- **本地调试走代理**：QQ 流量可通过 SSH SOCKS 转到 10.8.0.4；`BotClient` 已让 REST 与 WebSocket 都使用系统代理。数据库一般通过 SSH 隧道。详见[本地联调](#本地联调)。

## 代码地图

```
Sapphire/
├── Program.cs                  # 入口：根指令注册、启动顺序、全局 HttpClient/Timer
├── Sapphire.csproj             # net10.0；ProjectReference 指向 third_party/QQBot.Net
├── Commands/                   # 指令层
│   ├── CommandBase.cs          # 指令基类、CommandContext、FlagSpec、发消息/权限辅助
│   ├── MessageContext.cs       # 与接入协议无关的消息上下文（群/单聊、canonical 用户 ID）
│   ├── MaiCommandBase.cs       # maimai 指令共用：歌曲匹配、列表文案
│   ├── GroupMemberCommandBase.cs
│   ├── ChuniCommandBase.cs
│   ├── MaiCommand.cs           # 功能域根指令，SubCommands 挂具体指令
│   ├── MaiCommands/            # 具体 maimai 指令
│   ├── GroupMemberCommands/    # 群友指令
│   └── ChuniCommands/
├── Data/                       # 数据访问与领域数据
│   ├── Mai/{Database,Dtos}     # 曲目元数据 + 用户成绩（bytea，key 为 canonical openid）
│   ├── DivingFishMai/          # DivingFish OAuth / 上传 / 拉取
│   ├── Wck/                    # 华立 WCK（成绩同步，session 按 openid 索引）
│   ├── GroupMember/            # 群友别名、婚姻（别名按群存 canonical + scene openid）
│   ├── Settings/               # 群/单聊设置
│   └── Chuni/
├── Environments/               # config.toml 模型与单例读取
├── ImageGeneration/            # ImageSharp 绘图：调色板、字体、图片生成器
├── Migrations/                 # EF Core 迁移（按 DbContext 分目录）
├── UserSettings/               # 设置项定义（标识符、显示名、默认值）
└── Utils/                      # BotClient、MigrationServer、解析器、日志、Timer、搜索归一化、进制转换
```

独立项目：

```
Sapphire.Migration/             # 旧库迁移工具（独立进程，Npgsql + WebSocket 客户端）
├── Program.cs                  # 连接主 bot，接收迁移请求、回传结果
├── MigrationConfig.cs          # [Database] / [LegacyDatabase] / [Bot] 配置
├── MigrationProtocol.cs        # 与主 bot 的 WS JSON 协议
├── MigrationHandler.cs         # 旧库 → 新库的 SQL 迁移
└── config.example.toml         # 复制为 config.toml 后填写连接串
```

## 运行配置

配置来源是应用基目录下的 `config.toml`（`AppContext.BaseDirectory`），由 `Environments/SpEnvironmentHandler.cs` 在首次访问时自动生成默认文件，之后只读取。**配置写错导致反序列化失败时，整份文件会被默认配置覆盖写回**，修改前先备份。

节与关键字段：

- `[QQBot]`：`AppId`（int）、`AppSecret`（string）。官方平台「开发设置」里的 AppID / AppSecret。
- `[Database]`：`SqlConnectionString`（PostgreSQL）。
- `[DivingFish]`：`ClientId`、`ClientSecret`、`DivingFishAuthUrl`、`DivingFishUrl`。
- `[Mai]`：`CnLastestVersionIndexes`、`JpLastestVersionIndexes`。
- `[WahlapConnectiveKits]`：`WckUrl`（WCK 服务地址，默认 `http://10.8.0.2:10492`）。
- `[ImageGeneration]`：配色、`PreloadCoverImages`、`MaxParallelGenerationTaskCount`。
- `[Sapphire]`：`BotName`、`AdministratorOpenIds`（canonical openid 字符串数组）、`CommandPrefixes`、`UtcDayTickTime`。
- `[Migration]`：`Enabled`（默认 false）、`Port`（默认 5201）、`Token`、`AllowAllUsers`（默认 false）。启用后主 bot 在 `127.0.0.1:<Port>/migration/` 启动 WebSocket 服务端。
- `[NapCat]`、`[Sapphire] AdministratorQqId`、`[Sapphire] BotQqId`：**仅为兼容旧 config.toml 保留的反序列化字段，代码不再使用**；新配置不要填。

注意：

- `config.toml` 含 QQ Bot AppSecret、数据库口令、DivingFish ClientSecret，**不要提交**，也不要在日志或回复中打印。`Sapphire/bin/.../config.toml` 只是本地产物（`bin/` 已被忽略）。
- 修改配置模型时同步修改 `Environments/SubEnvironments/*.cs`；字段名即 TOML 键名（Tomlyn 默认 PascalCase）。
- 启动顺序：初始化 Serilog → `new SettingsDataManager()`（迁移设置库）→ 各根指令 `StartInitialize()`（建库/迁移、加载曲目索引、预载封面）→ `BotClient.RunAsync` 登录并启动网关 → 主线程 `keepAlive.WaitOne()`。
- QQBot.Net 自带断线重连；`BotClient` 只负责登录/启动、日志桥接、把 `MessageReceived` 转成 `MessageContext`。
- 缺少 PostgreSQL 或 QQBot 凭据时：Debug 下初始化/登录异常会直接抛出；Release 下记录错误并退出。排查「指令没反应」先检查数据库、AppID/AppSecret、机器人是否在群里并被 @。

## 指令系统

所有指令都继承 `CommandBase`。根指令在 `Program.cs` 的 `GroupCommands` 中注册；功能域指令（如 `MaiCommand`）把子指令放进 `SubCommands`，框架递归初始化与分发。

### 消息上下文（`Commands/MessageContext.cs`）

官方网关只给群消息下发「@ 机器人」事件（`GROUP_AT_MESSAGE_CREATE`），并且 **content 已去掉 @ 前缀**；C2C 单聊是 `C2C_MESSAGE_CREATE`。`BotClient` 负责：

- 过滤 guild/未知频道，只处理群与单聊；
- `Content.Trim()`，空消息忽略；
- 按 `message.Id` 去重（官方可能重复推送）；
- 生成 `MessageContext`。

`MessageContext` 关键字段：

| 字段 | 含义 |
| --- | --- |
| `IsGroup` | 群聊 / 单聊 |
| `ChatId` | 群聊为 `group_openid`，单聊为 `user_openid` |
| `SceneUserId` | 当前场景的 `member_openid` / `user_openid`，用于头像、群成员相关 |
| `UserId` | **canonical 用户 ID**；优先 `union_openid`，为空时回退到 `SceneUserId` |
| `MessageId` / `Content` | 原消息 ID / 已 trim 的文本 |
| `Channel` / `Message` | QQBot.Net 的 `IMessageChannel` / `IUserMessage`，被动回复用 |
| `UserName` / `GroupNickname` / `MemberRole` | 官方事件里的 `username` / `member_nickname` / `member_role` |
| `UnionOpenId` / `Avatar` | 官方事件里的 `union_openid` / 头像 URL |

**身份策略**：官方 `member_openid` 按群下发，同一用户在不同群、以及 C2C 里的 scene ID 不保证相同；`union_openid` 跨场景一致（本 bot 实测一致）。因此：

- 用户成绩 `MaiUserPlayRecords.UserId`、DivingFish `subject_ref`、WCK session 索引全部使用 `UserId`（canonical）。
- 群友别名/群成员表同时存 `UserId`（canonical）和 `SceneUserId`（member_openid）；别名按群独立，但解析到 canonical 后能读取跨群统一成绩。
- 如果某个 bot 的 `union_openid` 为空，canonical 会退化为 scene ID，跨群不统一；此时需要额外的绑定机制（当前代码没有实现）。
- `AdministratorOpenIds` 填 canonical openid；群内管理员另外可用事件里的 `MemberRole`（`owner` / `admin`）判断。

### 匹配规则（`Utils/CommandParser.cs`）

- 群聊里必须先 @ 机器人；content 已去掉 @ 前缀。单聊不需要 @。
- 前缀形式：`<prefix> <head> [subhead...] [args]`。`prefix` 必须精确等于 `Sapphire.CommandPrefixes` 中的一项（**区分大小写**），默认 `s` / `sapphire` / `saf`；例如 `s mai search 恋`。
- 逐 token 沿 `SubCommands` 下钻，命中即停，剩余 token 作为参数。
- 简短形式（lite）：`lite_commands.enabled` 为 true 时，第一个 token 与任一 `DirectCommandHeads` 忽略大小写匹配，例如 `搜歌 恋`、`b50`、`娶群友`。
- 前缀形式优先于 lite 形式；两者都没命中则静默忽略。
- 快捷别名（`CommandParser.Shortcuts`）：`score` → `info -s`、`ap50` → `b50 -ap`，以及 `app50` / `fc50` / `fcp50` / `pc50` / `f50`，消息以该词开头即替换（不检查词边界）。
- 解析前会先让所有根指令执行 `ReceiveDirectly`；`ConfirmationCommand` 依赖它处理「确定/取消」，不要把它从 `GroupCommands` 中移除。
- 每个指令受 `ActivationSettingsIdentifier` 控制，按群或单聊用户从设置库读取；未配置时默认启用。

### 参数与 Flag

- `SupportedFlags` 中每个 `FlagSpec` 声明短名/长名、是否带值。`TakesValue = true` 时，值会一直收集到下一个 flag 为止。
- 处理器里用**长名**查询：`commandContext.GetFlag("--bpm")`、`commandContext.HasFlag("--divingfish")`。`FlagSpec.Name` 就是 `LongName`，所以**一定要设置 `LongName`**，否则会存成空键。
- 没有 flag 且没有位置参数时走 `Process`，否则走 `ProcessWithArguments`。`Argument` 是剩余 token 用空格拼接的字符串，可能为 `null`。
- 输出统一用 `SendMessage(context, "text")` 或 `SendMessage(context, ["text"])`；图片用 `SendImage(context, base64)`。两者都是**被动回复**（`passiveSource: context.Message`），不再使用引用消息。
- 官方被动回复限制：群聊 5 分钟 / 每条消息 5 次，单聊 60 分钟 / 4 次。不要把文本和图片拼在一条消息里（官方 `msg_type` 只能选一种）；图片和文本会拆成两条。
- `CommandBase` 提供 `SendArgumentError` / `SendUnknownError` / `SendUnpermittedError`，mai 域另有 `SendUnauthorizedError` / `SendNotLoggedInError`。
- 权限：`IsBotAdministrator(context)` 比对 `Sapphire.AdministratorOpenIds`；`IsGroupAdministrator(context)` 优先用事件里的 `MemberRole`（`owner`/`admin`），否则回退到 bot 管理员配置。
- 确认流程：`ConfirmationCommand` 按 `(ChatId, TargetUserId)` 记录待确认项，目标用户直接回复「确定」/「取消」即可，**不需要引用原消息**；10 分钟过期。

### 新增一个指令

1. 在 `Commands/<域>/` 新建类，继承 `CommandBase` 或对应域基类（`MaiCommandBase` 等）。
2. 构造函数里设置 `CommandHeads`、`DirectCommandHeads`、`SupportedFlags`、`ActivationSettingsIdentifier`。
3. 按需重写 `Process` / `ProcessWithArguments` / `ProcessDirectly` / `Initialize` / `OnExit`。
4. 注册：根指令加到 `Program.GroupCommands`，或在父指令的 `SubCommands` 数组里追加。
5. 若需要开关：在 `UserSettings/SettingsItem.cs` 的 `SettingsItems.Categories` 加显示项，并在 `DefaultValues` 加默认值。漏加 `DefaultValues` 时不会出现在 `settings` 列表，且默认按启用处理。
6. 运行 `dotnet build`，再按[验证与提交](#验证与提交)手工联调。

示例：

```csharp
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

    protected override async Task Process(MessageContext context)
    {
        await SendMessage(context, "用法：example <文本>");
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (commandContext.Argument is null)
        {
            await SendArgumentError(context);
            return;
        }

        await SendMessage(context, $"收到：{commandContext.Argument}");
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
| `SettingsMetadataContext` | `Data/Settings/Database` | `Migrations/` | 群/单聊设置项，`ChatId` 为 openid 字符串 |
| `GroupMemberDataContext` | `Data/GroupMember/Database` | `Migrations/GroupMemberData/` | 群友、别名、婚姻；字符串 ID，别名同时存 canonical/scene |
| `MaiMetadataContext` | `Data/Mai/Database` | 无 | 曲目/谱面/别名元数据，仓库外维护 |
| `MaiUserDataContext` | `Data/Mai/Database` | `Migrations/MaiUserData/` | 用户成绩 blob，`UserId` 为 canonical openid |
| `ChuniMetadataContext` | `Data/Chuni/Database` | 无 | 中二曲目元数据，仓库外维护 |

- 启动时各 Manager 构造函数调用 `Database.Migrate()`，正常部署不需要手工建表。
- `MaiMetadataContext` 与 `ChuniMetadataContext` 在仓库内**没有迁移**：曲目元数据由外部导入，`Migrate()` 不会创建 `MaiSongs` 等业务表。改这两个上下文前先确认真实库结构由谁维护，不要用迁移去「补建表」。
- 用户 ID 统一 `varchar(64)`；`GroupMemberAliases` 与 `GroupMembers` 额外有 `SceneUserId`。`MaiAliases.CreatorUserId` 是 `text`（可空），旧别名数据的 creator 已置 `NULL`。
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
- 从旧 NapCat 版迁移时：歌曲/谱面/版本/分类/歌曲别名可以直接复制；用户成绩、设置、群友别名/婚姻的旧主键是 QQ 号，无法映射到 openid，测试环境选择重新生成/重新绑定。
- 时间统一用 UTC（`DateTime.UtcNow`、`UtcDayTickTime`）；「自然日」边界由 `UtcDayTickTime` 决定。

## 旧库迁移

主 bot 侧：

- `[Migration] Enabled = true` 时，`MigrationServer` 在 `127.0.0.1:<Port>/migration/` 启动 WebSocket 服务端；`Port` 默认 5201，`Token` 可选。
- 群聊指令 `migrate <QQ号> --group <旧群号>`；**群聊里 `--group` 必填**，值是旧 NapCat 库里的 QQ 群号，不是新 `group_openid`。
- 私聊指令 `migrate <QQ号>`；不传 `--group`，只迁移成绩和私聊设置，不导入别名/群成员/群设置，允许普通用户自行执行。
- 群聊迁移默认只有 bot 管理员/群主/群管理员能执行；`[Migration] AllowAllUsers = true` 才允许所有群成员。QQ 号本身没有二次校验，开启前确认群环境可信。
- 请求携带 `QqNumber / CanonicalUserId / SceneUserId / ChatId / LegacyGroupId`；迁移项目完成后回传计数，主 bot 调 `GroupMemberDataManager.ReloadAndBake()` 和 `SettingsDataManager.Reload()`，别名/群成员/设置立即生效。
- 主 bot 在 `UserMigrationStatus` 表记录已迁移用户（表由 `UserMigrationStatusManager` 用 Npgsql 按需创建，不走 EF 迁移）；`娶群友` / `透群友` 在未迁移时发送填写模板并拦截。
- 首次创建状态表时，会把已有 `MaiUserPlayRecords` / `GroupMemberAliases` 的用户种子标记为已迁移；新用户需要先执行一次 `migrate`（旧库没有数据也会以 0 条成功并标记）。

未迁移时 `娶群友` / `透群友` 的提示模板：

```text
使用「娶群友 / 透群友」前，请先迁移旧数据。

群聊：先 @ 我，再发送
migrate <你的QQ号> --group <当前群对应的旧QQ群号>

私聊：发送
migrate <你的QQ号>

把 <> 里的内容换成实际数字；迁移完成后会自动放行。
示例：migrate 123456789 --group 987654321
```

迁移项目 `Sapphire.Migration`：

- 独立进程，配置是它输出目录下的 `config.toml`（从 `config.example.toml` 复制），包含：
  - `[Database]` 新库连接串
  - `[LegacyDatabase]` 旧 NapCat 库连接串
  - `[Bot] WebSocketUrl`（默认 `ws://127.0.0.1:5201/migration/`）和 `Token`（与主 bot 的 `[Migration] Token` 一致）
- 用 Npgsql 直接读写，不引用 EF 模型；启动后连接主 bot，断线每 5 秒重连。
- 迁移内容：
  - 旧 `MaiUserPlayRecords`（按 QQ）→ 新 `MaiUserPlayRecords`（按 canonical）；新库已有记录时**覆盖**。
  - 旧 `GroupMemberAliases` 中 `GroupId = 旧群号` 的别名 → 新库当前 `ChatId` 群，`UserId = canonical`，`SceneUserId = 当前 scene openid`；重复别名跳过（仅群聊迁移）。
  - 旧 `GroupMemberAliases` 对应的用户 → 新 `GroupMembers` 当前群成员池（不存在时插入；仅群聊迁移）。
  - 旧 `SettingsItems`：单聊设置（`IsGroup=false, ChatId=QQ`）→ 新库 `IsGroup=false, ChatId=canonical`，私聊迁移始终执行；群设置（`ChatId=旧群号`）→ 新库 `IsGroup=true, ChatId=当前群 openid`，仅群聊迁移且需要 `--group`；相同主键 upsert。
  - 婚姻不迁移：旧库 `PartnerId` 也是 QQ 号，需要双方 QQ→canonical 映射，目前没有该映射表。
- 同一个 QQ 可以对多个旧群分别执行迁移；成绩会被重复覆盖，别名会合并到当前群。

## 图片生成与发送

- `ImageGeneration/ColorPalette.cs` 静态构造时从 `AppContext.BaseDirectory/Resources/Fonts/` 读取 `light.otf`、`normal.otf`、`regular.otf`、`heavy.otf`、`fallback.ttf`、`emoji.ttf`；缺文件会在首次画图时抛异常。
- 封面：`Resources/Covers/<sapphireId>.png`、`Resources/CoversHd/<sapphireId>.png`，缺失时回退到 `0.png`。`PreloadCoverImages = true` 时启动即把 `Resources/Covers` 全部载入内存（`MaiImageGeneratorFunctions.LoadImages`）。
- 这些 `Resources/` 运行时资源**不在 git 中**，需要本地或部署环境自行提供；`dotnet build` 不会自动复制。
- 出图统一走 `ImageGenerationHandler.Instance.AddTask(...)`，由信号量按 `MaxParallelGenerationTaskCount` 限流；返回 base64 JPEG（质量 50）。
- `CommandBase.SendImage` 把 base64 转成 `FileAttachment(ReadOnlyMemory<byte>)`，由 QQBot.Net 上传富媒体后发送；**文本和图片是两条消息**，注意被动回复次数。
- 头像 URL 由 `ImageGeneratorFunctions` 拼成 `https://q.qlogo.cn/qqapp/{AppId}/{openId}/{size}`；openId 传 `SceneUserId`（群内 `member_openid` / 单聊 `user_openid`）。
- ImageSharp 对象实现 `IDisposable`，生成器里注意 `using`；`PingCommand` 会报告未释放的 Image 数量，可用于排查泄漏。
- `Sapphire/sixlabors.lic` 是 SixLabors 许可凭证（Community，2028-01-19 到期）；升级 ImageSharp 前确认许可要求。

## 本地联调

测试机为 `10.8.0.4`（Linux，root 密钥登录），QQ 官方 API 出口 IP 为 `47.104.221.55`；`10.8.0.4` 只是 WireGuard 内网地址。官方控制台的 IP 白名单如果按出口 IP 校验，应放行公网出口，而不是 `10.8.0.4`。

```bash
# QQ 流量走 10.8.0.4 的 SOCKS
ssh -N -D 1080 root@10.8.0.4

# PostgreSQL（服务端只监听 127.0.0.1）
ssh -N -L 5432:127.0.0.1:5432 root@10.8.0.4

export HTTPS_PROXY=socks5://127.0.0.1:1080
export HTTP_PROXY=socks5://127.0.0.1:1080
export ALL_PROXY=socks5://127.0.0.1:1080
export NO_PROXY=127.0.0.1,localhost,10.8.0.2   # 数据库、WCK 直连
dotnet run --project Sapphire/Sapphire.csproj
```

- `BotClient` 已配置 `DefaultRestClientProvider.Create(useProxy: true)` 和 `DefaultWebSocketProvider.Create(HttpClient.DefaultProxy)`。QQBot.Net 默认 REST 是 `UseProxy = false`，不设置会从本机直连并报 `11298 接口访问源IP不在白名单`。
- WCK 服务在 `10.8.0.2`（Linux arm64，systemd `lapis-wahlap.service`，代码在仓库 `LapisBot.WahlapConnectiveKits`）；手机/本机调试时需要 `NO_PROXY` 放行。
- 测试群里 @ 机器人后发 `whoami` 可以打印 `ChatId` / `CanonicalUserId` / `SceneUserId` / `UnionOpenId` / 群角色，用来配置 `AdministratorOpenIds` 或排查身份。

## 编码约定

- 语言特性：file-scoped namespace；`ImplicitUsings` 与 `Nullable` 全开；优先使用 collection expressions（`[...]`）、`required` + `init`、record 承载配置/DTO。
- 格式：4 空格缩进，Allman 大括号；私有字段 `_camelCase`（`static readonly` 字段多为 PascalCase，跟随周边文件）；异步方法加 `Async` 后缀，返回 `Task` / `Task<T>`。
- 命名：类型/成员 PascalCase；指令类以 `Command` 结尾；设置标识符形如 `<域>_<功能>.enabled`（如 `mai_b50.enabled`）。
- 面向用户的文案用中文，代码标识符用英文；注释中英混用，跟随文件现状。
- Manager 使用 `public static XxxDataManager? Instance { get; private set; }` 单例，索引烘焙完成后再赋值；重建索引调用 `BakeIndexingData()`。访问 `Instance` 前判空，避免初始化失败时 NRE。
- 全局状态集中在 `Program`（`HttpClient`、`Timer`）、`BotClient` 和各 Manager；新增共享可变状态必须加锁（参考 `MarryCommand` 的 `Lock`、`GroupMemberDataManager` 的索引重建）。
- 网络请求复用 `Program.HttpClient`，不要每次 `new HttpClient()`；HTTP/JSON 异常在边界捕获并用 Serilog 记录，Debug 下按现有模式 `throw`。
- 不在消息或日志中泄露令牌、连接串、ClientSecret。

## 验证与提交

- 当前没有自动化测试。改动后至少执行 `dotnet build Sapphire.sln`，保持 0 warning / 0 error。
- 涉及指令、数据库、图片的改动需要 QQ 官方 bot + PostgreSQL + 本地 `Resources/` +（WCK 相关）WCK 服务才能端到端验证。无法联调时在回复中说明未验证项，不要声称「已测试」。
- 官方群聊只下发 @ 机器人的消息；测试时必须在群里 @ 机器人。文本/图片/被动回复次数限制见上。
- 不要提交：`bin/`、`obj/`、`config.toml`、`logs/`、`.idea/`、`*.DotSettings.user`、本地生成的 `Resources/`（如需入库先与维护者确认）。
- 提交信息沿用历史风格：`feat:` / `fix:` + 英文简述（冒号后空一格，句首大写），尽量一次提交聚焦一件事；可参考 `git log --oneline`。
- 改动跨指令/数据层时，确认没有破坏 `Program.GroupCommands` 的注册和设置项默认值。
- 修改项目结构、命令注册方式、配置字段或构建命令后，同步更新本文件。

## 易错点

- **群消息只有 @ 机器人时才会收到**，且 content 已去掉 @ 前缀；不要按 NapCat 的「普通消息 + 前缀」习惯测试。
- 被动回复：群 5 分钟/5 次，单聊 60 分钟/4 次；图片和文本分两条发送，连续多条错误提示可能耗尽额度。
- `CommandBase.Receive` 的分支依据是「有没有 flag / 有没有位置参数」；只重写 `Process` 会导致带参数时落到基类默认回复 `dame`。
- `CommandHeads` 与 `DirectCommandHeads` 是两套；只配一个时另一种调用形式不可用。
- 前缀匹配区分大小写，lite 匹配不区分；`s` 与 `S` 不等价。
- 设置项按「群」或「单聊 canonical 用户」存储；私聊里改的设置不影响群。
- `SettingsDataManager` 构造时把整张设置表读进内存，`SetValue` 会同时更新内存和数据库；绕过它直接改库不会即时生效。
- `MaiDataManager` 的索引是常驻内存的；曲目或别名变化后需要重新烘焙（`BakeIndexingData`）或重启进程。
- `GroupMemberDataManager.GetMarriages()` 会先删除当天之前的记录，依赖 `UtcDayTickTime`；改动婚姻逻辑时注意这个副作用。
- `GroupMemberDataManager.SceneUserIds` / `DisplayNames` 是进程内缓存，重启后需要用户再次发言才会重新积累；群成员列表接口目前是内邀（11253），不要假设能随时查任意成员。
- 官方 `union_openid` 可能为空；`MessageContext.UserId` 会退化为 scene ID，跨群成绩会分裂。换 bot 后先用 `whoami` 确认。
- WCK 服务按 `openid` 索引 session（不是 QQ 号）；Sapphire 侧 `SessionLoginRequestDto` / `WckDataManager` 已同步，改动时两边一起改。
- QQBot.Net 0.2.0 不解析 `username` / `member_nickname` / `member_role` / `union_openid`；本项目依赖 `third_party/QQBot.Net` fork 的 `SocketMessage.Author*` 属性。升级 SDK 时先同步 fork，不要直接换回 NuGet 包。
- `MaiAliases.CreatorUserId` 在新库是 `text`；旧别名 creator 已置 `NULL`，只有 bot 管理员能删除旧别名。
- 群聊 `migrate` 的 `--group` 必填，值是**旧库 QQ 群号**；不要传新 `group_openid`。私聊 `migrate` 不需要也不使用 `--group`，只迁移成绩和私聊设置。
- 同一个 QQ 对多个旧群分别迁移会重复覆盖成绩、把别名合并到当前群。
- 迁移服务必须先在主 bot 的 `[Migration] Enabled = true` 且 `Sapphire.Migration` 已连接；否则指令只会回“迁移服务未启用/未连接”。
- 迁移项目直接写新库，主 bot 的内存索引不会自动更新；必须等回调里的 `ReloadAndBake()`/`Reload()` 执行。手工改库后也要重启或触发 reload。
- 迁移会**覆盖**新库已有的 `MaiUserPlayRecords`；如果用户已经用 DivingFish/WCK 更新过成绩，先确认是否还要旧数据。婚姻不迁移。
- `Sapphire.Migration` 的 `config.toml` 含新旧库口令，只放在输出目录（`bin/` 已忽略），不要提交；`config.example.toml` 可以提交。
- `UserMigrationStatus` 是主 bot 自动建的表，不是 EF 迁移；手工清空它会让所有用户重新收到娶/日迁移提醒。已有成绩/别名的用户在表首次创建时会被自动标记。
- 新用户第一次娶/日会被拦截并要求先 migrate；迁移成功后才写入 `UserMigrationStatus`。旧库没有数据时也建议跑一次 `migrate`，以 0 条结果完成标记。

## 参考

- 远程仓库：<https://github.com/Windoooors/Sapphire>
- QQBot.Net fork：<https://github.com/Windoooors/QQBot.Net>（上游：<https://github.com/gehongyan/QQBot.Net>；文档：<https://qqbotnet.dev/>）
- 官方文档：<https://bot.q.qq.com/wiki/develop/api-v2/>
