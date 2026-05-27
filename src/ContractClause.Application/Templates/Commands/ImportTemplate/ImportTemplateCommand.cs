using MediatR;

namespace ContractClause.Application.Templates.Commands.ImportTemplate;

public record ImportTemplateCommand(
    string HtmlContent,
    string Title,
    string Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    bool IsOfficial,
    Guid? OwnerId) : IRequest<ImportTemplateResult>;

public record ImportTemplateResult(Guid TaskId, string Status, string Message);
