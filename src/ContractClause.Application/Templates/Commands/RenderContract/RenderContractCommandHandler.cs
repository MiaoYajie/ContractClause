using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Templates.Commands.RenderContract;

public class RenderContractCommandHandler(ITemplateRepository templates)
    : IRequestHandler<RenderContractCommand, RenderContractResultDto?>
{
    public async Task<RenderContractResultDto?> Handle(RenderContractCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(request.TemplateId, ct);
        if (template is null) return null;

        var required = VariableHelper.Extract(template.ContentMarkdown);
        var missing = required.Where(v => !request.Variables.ContainsKey(v)).ToList();
        var content = VariableHelper.Render(template.ContentMarkdown, request.Variables);

        return new RenderContractResultDto(
            template.Id,
            request.Format,
            content,
            missing);
    }
}
