using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Templates.Queries.GetTemplateVariables;

public record GetTemplateVariablesQuery(Guid TemplateId) : IRequest<IReadOnlyList<VariableInfoDto>?>;
