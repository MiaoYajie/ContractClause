namespace ContractClause.Application.Common.Interfaces;

public interface IUserContext
{
    Guid? OwnerId { get; }
    string? OwnerType { get; }
    bool IsAuthenticated { get; }
}
