using MediatR;

namespace ContractClause.Application.Templates.Commands.DeleteTemplate;

public record DeleteTemplateCommand(Guid Id) : IRequest<bool>;
