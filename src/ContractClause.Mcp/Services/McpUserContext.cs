using ContractClause.Application.Common.Interfaces;

namespace ContractClause.Mcp.Services;

public class McpUserContext(McpAuthState authState) : IUserContext
{
    public Guid? OwnerId => authState.OwnerId;
    public string? OwnerType => authState.OwnerType;
    public bool IsAuthenticated => authState.IsAuthenticated;
}
