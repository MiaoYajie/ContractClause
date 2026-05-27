using MediatR;

namespace ContractClause.Application.Templates.Commands.UpdateTemplate;

public record UpdateTemplateCommand(
    Guid Id,
    string? Title,
    string? Type,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Tags,
    string? Summary,
    string? Scenarios) : IRequest<bool>;
