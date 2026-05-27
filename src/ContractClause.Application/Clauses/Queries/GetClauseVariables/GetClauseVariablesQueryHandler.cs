using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Clauses.Queries.GetClauseVariables;

public class GetClauseVariablesQueryHandler(IClauseRepository clauses)
    : IRequestHandler<GetClauseVariablesQuery, IReadOnlyList<VariableInfoDto>?>
{
    public async Task<IReadOnlyList<VariableInfoDto>?> Handle(GetClauseVariablesQuery request, CancellationToken ct)
    {
        var clause = await clauses.GetByIdAsync(request.ClauseId, ct);
        if (clause is null) return null;

        var names = clause.Variables.Count > 0 ? clause.Variables : VariableHelper.Extract(clause.Text);
        return names.Select(n => new VariableInfoDto(n, $"请填写{n.Trim('{', '}')}", true)).ToList();
    }
}
