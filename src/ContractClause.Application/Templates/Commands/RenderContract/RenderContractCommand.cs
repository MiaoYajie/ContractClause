using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Templates.Commands.RenderContract;

public record RenderContractCommand(
    Guid TemplateId,
    IReadOnlyDictionary<string, string> Variables,
    string Format) : IRequest<RenderContractResultDto?>;
