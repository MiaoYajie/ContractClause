namespace ContractClause.Application.Common.DTOs;

public record TemplateSearchItemDto(
    Guid Id,
    int Number,
    string Title,
    string Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    string Summary,
    bool IsOfficial,
    double Score);

public record PagedTemplateSearchResultDto(
    int Total,
    int Page,
    int PageSize,
    string SearchMode,
    IReadOnlyList<TemplateSearchItemDto> Items);

public record OutlineItemDto(
    string Id,
    string Title,
    int Level,
    IReadOnlyList<string> Variables,
    IReadOnlyList<OutlineItemDto> Children);

public record OutlineResultDto(Guid TemplateId, IReadOnlyList<OutlineItemDto> Outline);

public record VariableInfoDto(string Name, string Description, bool Required);

public record RenderContractResultDto(Guid TemplateId, string Format, string Content, IReadOnlyList<string> MissingVariables);
