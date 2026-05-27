using ContractClause.Application.Common.Interfaces;

namespace ContractClause.Api.Services;

public class ApiUserContext : IUserContext
{
    public Guid? OwnerId { get; set; }
    public string? OwnerType { get; set; }
    public bool IsAuthenticated { get; set; }
}
