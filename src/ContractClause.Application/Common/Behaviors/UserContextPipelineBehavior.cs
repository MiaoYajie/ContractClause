using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Common.Behaviors;

public class UserContextPipelineBehavior<TRequest, TResponse>(
    IUserContext userContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        next();
}
