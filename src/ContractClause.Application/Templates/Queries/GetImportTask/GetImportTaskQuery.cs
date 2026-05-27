using MediatR;

namespace ContractClause.Application.Templates.Queries.GetImportTask;

public record ImportTaskStatusDto(
    Guid TaskId,
    string Status,
    Guid? TemplateId,
    int ClausesImported,
    IReadOnlyList<string> Errors);

public record GetImportTaskQuery(Guid TaskId) : IRequest<ImportTaskStatusDto?>;
