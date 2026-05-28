using System.Text.Json;
using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Templates;
using MediatR;

namespace ContractClause.Application.Templates.Queries.GetTemplateVariables;

public class GetTemplateVariablesQueryHandler(
    ITemplateRepository templates,
    IClauseRepository clauses) : IRequestHandler<GetTemplateVariablesQuery, IReadOnlyList<VariableInfoDto>?>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<VariableInfoDto>?> Handle(GetTemplateVariablesQuery request, CancellationToken ct)
    {
        var template = await templates.GetByIdWithOutlineAsync(request.TemplateId, ct);
        if (template is null) return null;

        var names = new HashSet<string>();
        if (template.Outline is not null)
        {
            var items = JsonSerializer.Deserialize<List<OutlineItem>>(template.Outline.OutlineJson, JsonOptions) ?? [];
            CollectFromOutline(items, names);
        }

        var clauseList = await clauses.ListByTemplateIdAsync(template.Id, ct);
        foreach (var clause in clauseList)
        {
            foreach (var v in VariableHelper.Extract(clause.Text))
                names.Add(v);
        }

        return names.OrderBy(n => n).Select(n => new VariableInfoDto(n, Describe(n), true)).ToList();
    }

    private static void CollectFromOutline(IEnumerable<OutlineItem> items, HashSet<string> names)
    {
        foreach (var item in items)
        {
            foreach (var v in item.Variables) names.Add(v);
            CollectFromOutline(item.Children, names);
        }
    }

    private static string Describe(string name) =>
        name.Trim('{', '}') switch
        {
            "合同日期" => "合同签署日期",
            "合同编号" => "合同唯一编号",
            _ => $"请填写{name.Trim('{', '}')}"
        };
}
