namespace Sapphire.Environments.SubEnvironments;

public record MigrationEnvironment
{
    /// <summary>
    ///     是否启动本地 WebSocket 迁移服务端。
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    ///     仅监听 127.0.0.1。
    /// </summary>
    public int Port { get; init; } = 5201;

    /// <summary>
    ///     可选共享令牌；迁移项目连接时通过 <c>?token=</c> 传入。
    /// </summary>
    public string Token { get; init; } = "";

    /// <summary>
    ///     默认仅 bot 管理员/群管理员可以执行迁移；设为 true 后允许所有群成员。
    /// </summary>
    public bool AllowAllUsers { get; init; }
}
