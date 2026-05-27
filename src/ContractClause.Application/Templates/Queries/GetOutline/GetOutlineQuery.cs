using ContractClause.Application.Common.DTOs;
using MediatR;

namespace ContractClause.Application.Templates.Queries.GetOutline;

public record GetOutlineQuery(Guid TemplateId) : IRequest<OutlineResultDto?>;
