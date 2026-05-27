namespace ContractClause.Application.Common.DTOs;

public record ClauseItemDto(
    Guid Id,
    Guid? TemplateId,
    string? OutlineItemId,
    string ClauseType,
    string Text,
    IReadOnlyList<string> Variables,
    IReadOnlyList<string> Keywords,
    double Score);

public record PagedClauseSearchResultDto(
    int Total,
    IReadOnlyList<ClauseItemDto> Items,
    string? SearchMode = null);
