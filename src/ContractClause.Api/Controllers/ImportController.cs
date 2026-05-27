using ContractClause.Application.Templates.Commands.ImportTemplate;
using ContractClause.Application.Templates.Queries.GetImportTask;
using ContractClause.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ContractClause.Api.Controllers;

[ApiController]
[Route("api/v1/import")]
public class ImportController(IMediator mediator, ApiUserContext userContext) : ControllerBase
{
    [HttpPost("template")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> ImportTemplate(
        IFormFile file,
        [FromForm] string title,
        [FromForm] string type,
        [FromForm] string? categories,
        [FromForm] string? tags,
        [FromForm] bool isOfficial = false,
        CancellationToken ct = default)
    {
        if (file.Length == 0)
            return BadRequest(new { code = "INVALID_FILE", message = "文件不能为空" });

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var html = await reader.ReadToEndAsync(ct);

        var cats = ParseList(categories);
        var tagList = ParseList(tags);

        var result = await mediator.Send(new ImportTemplateCommand(
            html, title, type, cats, tagList, isOfficial, userContext.OwnerId), ct);

        return Accepted(new { result.TaskId, result.Status, result.Message });
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<ActionResult<ImportTaskStatusDto>> GetTask(Guid taskId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetImportTaskQuery(taskId), ct);
        return result is null
            ? NotFound(new { code = "TASK_NOT_FOUND", message = $"任务 {taskId} 不存在" })
            : Ok(result);
    }

    private static IReadOnlyList<string> ParseList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
