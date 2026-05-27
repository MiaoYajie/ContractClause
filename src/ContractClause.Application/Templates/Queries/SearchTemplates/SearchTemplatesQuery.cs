using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Templates.Queries.SearchTemplates;

public record SearchTemplatesQuery(
    string Q,
    string? Type,
    IReadOnlyList<string>? Categories,
    bool? IsOfficial,
    string Sort,
    int Page,
    int PageSize) : IRequest<PagedTemplateSearchResultDto>;
