namespace ContractClause.Mcp.Services;

public class McpAuthState
{
    public Guid? OwnerId { get; set; }
    public string? OwnerType { get; set; }
    public bool IsAuthenticated { get; set; }
}
