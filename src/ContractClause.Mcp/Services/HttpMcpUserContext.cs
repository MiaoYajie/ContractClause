using ContractClause.Application.Common.Interfaces;

namespace ContractClause.Mcp.Services;

/// <summary>
/// HTTP 远程模式：从当前请求的 HttpContext 读取鉴权后的用户上下文。
/// </summary>
public class HttpMcpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private McpAuthState? AuthState =>
        httpContextAccessor.HttpContext?.Items[nameof(McpAuthState)] as McpAuthState;

    public Guid? OwnerId => AuthState?.OwnerId;
    public string? OwnerType => AuthState?.OwnerType;
    public bool IsAuthenticated => AuthState?.IsAuthenticated ?? false;
}
