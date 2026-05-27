using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Templates.Commands.DeleteTemplate;
using ContractClause.Application.Templates.Commands.RenderContract;
using ContractClause.Application.Templates.Commands.UpdateTemplate;
using ContractClause.Application.Templates.Queries.GetOutline;
using ContractClause.Application.Templates.Queries.GetTemplateVariables;
using ContractClause.Application.Templates.Queries.SearchTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContractClause.Api.Controllers;

[ApiController]
[Route("api/v1/templates")]
public class TemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<PagedTemplateSearchResultDto>> Search(
        [FromQuery] string q,
        [FromQuery] string? type,
        [FromQuery] string[]? categories,
        [FromQuery] bool? isOfficial,
        [FromQuery] string sort = "relevance",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { code = "INVALID_QUERY", message = "q 为必填参数" });

        var result = await mediator.Send(new SearchTemplatesQuery(q, type, categories, isOfficial, sort, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/outline")]
    public async Task<ActionResult<OutlineResultDto>> GetOutline(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOutlineQuery(id), ct);
        return result is null ? NotFound(new { code = "TEMPLATE_NOT_FOUND", message = $"模板 {id} 不存在" }) : Ok(result);
    }

    [HttpGet("{id:guid}/variables")]
    public async Task<ActionResult<object>> GetVariables(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTemplateVariablesQuery(id), ct);
        return result is null
            ? NotFound(new { code = "TEMPLATE_NOT_FOUND", message = $"模板 {id} 不存在" })
            : Ok(new { templateId = id, variables = result });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest body, CancellationToken ct)
    {
        var ok = await mediator.Send(new UpdateTemplateCommand(
            id, body.Title, body.Type, body.Categories, body.Tags, body.Summary, body.Scenarios), ct);
        return ok ? NoContent() : NotFound(new { code = "TEMPLATE_NOT_FOUND", message = $"模板 {id} 不存在" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new DeleteTemplateCommand(id), ct);
        return ok ? NoContent() : NotFound(new { code = "TEMPLATE_NOT_FOUND", message = $"模板 {id} 不存在" });
    }

    [HttpPost("{id:guid}/render")]
    public async Task<ActionResult<RenderContractResultDto>> Render(
        Guid id, [FromBody] RenderContractRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new RenderContractCommand(id, body.Variables, body.Format ?? "markdown"), ct);
        return result is null
            ? NotFound(new { code = "TEMPLATE_NOT_FOUND", message = $"模板 {id} 不存在" })
            : Ok(result);
    }
}

public record UpdateTemplateRequest(
    string? Title, string? Type, IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Tags, string? Summary, string? Scenarios);

public record RenderContractRequest(
    Dictionary<string, string> Variables, string? Format);
