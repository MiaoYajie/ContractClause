namespace ContractClause.Domain.ApiKeys;

public class ApiKey
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerType { get; set; } = "User";
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? CreatedBy { get; set; }
}
