using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Clauses.Queries.GetClauseVariables;

public record GetClauseVariablesQuery(Guid ClauseId) : IRequest<IReadOnlyList<VariableInfoDto>?>;
