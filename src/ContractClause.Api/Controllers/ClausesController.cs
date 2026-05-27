using ContractClause.Application.Clauses.Queries.GetClauseVariables;
using ContractClause.Application.Clauses.Queries.GetClauses;
using ContractClause.Application.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContractClause.Api.Controllers;

[ApiController]
[Route("api/v1/clauses")]
public class ClausesController(IMediator mediator) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<PagedClauseSearchResultDto>> Search(
        [FromQuery] string? q,
        [FromQuery] Guid? templateId,
        [FromQuery] string? clauseType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetClausesQuery(q, templateId, null, clauseType, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/variables")]
    public async Task<ActionResult<object>> GetVariables(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetClauseVariablesQuery(id), ct);
        return result is null
            ? NotFound(new { code = "CLAUSE_NOT_FOUND", message = $"条款 {id} 不存在" })
            : Ok(new { clauseId = id, variables = result });
    }
}
