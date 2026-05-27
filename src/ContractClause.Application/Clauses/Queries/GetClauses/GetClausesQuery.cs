using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Clauses.Queries.GetClauses;

public record GetClausesQuery(
    string? Q,
    Guid? TemplateId,
    string? OutlineItemId,
    string? ClauseType,
    int Page,
    int PageSize) : IRequest<PagedClauseSearchResultDto>;
