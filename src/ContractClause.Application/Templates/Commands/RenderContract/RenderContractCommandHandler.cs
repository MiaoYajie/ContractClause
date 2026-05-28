using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Templates.Commands.RenderContract;

public class RenderContractCommandHandler(
    ITemplateRepository templates,
    IClauseRepository clauses) : IRequestHandler<RenderContractCommand, RenderContractResultDto?>
{
    public async Task<RenderContractResultDto?> Handle(RenderContractCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(request.TemplateId, ct);
        if (template is null) return null;

        var clauseList = await clauses.ListByTemplateIdAsync(template.Id, ct);
        var source = clauseList.Count > 0
            ? string.Join("\n\n", clauseList.Select(c => c.Text))
            : string.Empty;

        var required = VariableHelper.Extract(source);
        var missing = required.Where(v => !request.Variables.ContainsKey(v)).ToList();
        var content = VariableHelper.Render(source, request.Variables);

        return new RenderContractResultDto(
            template.Id,
            request.Format,
            content,
            missing);
    }
}
