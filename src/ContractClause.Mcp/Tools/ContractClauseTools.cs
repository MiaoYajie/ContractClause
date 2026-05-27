using System.ComponentModel;
using System.Text.Json;
using ContractClause.Application.Clauses.Queries.GetClauseVariables;
using ContractClause.Application.Clauses.Queries.GetClauses;
using ContractClause.Application.Templates.Commands.RenderContract;
using ContractClause.Application.Templates.Queries.GetOutline;
using ContractClause.Application.Templates.Queries.GetTemplateVariables;
using ContractClause.Application.Templates.Queries.SearchTemplates;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractClause.Mcp.Tools;

[McpServerToolType]
public class ContractClauseTools(IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [McpServerTool, Description("搜索合同模板（语义 + 关键词混合）")]
    public async Task<string> SearchTemplates(
        [Description("搜索关键词或对合同场景的自然语言描述")] string query,
        [Description("模板类型过滤")] string? type = null,
        [Description("分类过滤")] string[]? categories = null,
        [Description("返回数量，默认 5，最大 20")] int limit = 5)
    {
        limit = Math.Clamp(limit, 1, 20);
        var result = await mediator.Send(new SearchTemplatesQuery(query, type, categories, null, "relevance", 1, limit));
        return JsonSerializer.Serialize(result.Items, JsonOptions);
    }

    [McpServerTool, Description("获取指定模板的合同大纲")]
    public async Task<string> GetOutline(
        [Description("模板 ID（Guid）")] string template_id)
    {
        if (!Guid.TryParse(template_id, out var id))
            return JsonSerializer.Serialize(new { error = "无效的 template_id" });

        var result = await mediator.Send(new GetOutlineQuery(id));
        return result is null
            ? JsonSerializer.Serialize(new { error = "模板不存在" })
            : JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool, Description("按条件获取合同条款")]
    public async Task<string> GetClauses(
        [Description("对所需条款的语义描述")] string? query = null,
        [Description("限定在某模板内搜索")] string? template_id = null,
        [Description("指定大纲项 ID")] string? outline_item_id = null,
        [Description("条款类型")] string? clause_type = null,
        [Description("返回数量，默认 5，最大 20")] int limit = 5)
    {
        limit = Math.Clamp(limit, 1, 20);
        Guid? templateId = Guid.TryParse(template_id, out var tid) ? tid : null;
        var result = await mediator.Send(new GetClausesQuery(query, templateId, outline_item_id, clause_type, 1, limit));
        return JsonSerializer.Serialize(result.Items, JsonOptions);
    }

    [McpServerTool, Description("获取模板的所有变量占位符")]
    public async Task<string> GetTemplateVariables(
        [Description("模板 ID（Guid）")] string template_id)
    {
        if (!Guid.TryParse(template_id, out var id))
            return JsonSerializer.Serialize(new { error = "无效的 template_id" });

        var result = await mediator.Send(new GetTemplateVariablesQuery(id));
        return JsonSerializer.Serialize(result ?? [], JsonOptions);
    }

    [McpServerTool, Description("获取指定条款的变量占位符")]
    public async Task<string> GetClauseVariables(
        [Description("条款 ID（Guid）")] string clause_id)
    {
        if (!Guid.TryParse(clause_id, out var id))
            return JsonSerializer.Serialize(new { error = "无效的 clause_id" });

        var result = await mediator.Send(new GetClauseVariablesQuery(id));
        return JsonSerializer.Serialize(result ?? [], JsonOptions);
    }

    [McpServerTool, Description("将模板与变量值组装，渲染为完整合同正文")]
    public async Task<string> RenderContract(
        [Description("模板 ID（Guid）")] string template_id,
        [Description("变量名到值的映射")] Dictionary<string, string> variables)
    {
        if (!Guid.TryParse(template_id, out var id))
            return JsonSerializer.Serialize(new { error = "无效的 template_id" });

        var result = await mediator.Send(new RenderContractCommand(id, variables, "markdown"));
        if (result is null)
            return JsonSerializer.Serialize(new { error = "模板不存在" });

        return JsonSerializer.Serialize(new
        {
            content = result.Content,
            missingVariables = result.MissingVariables
        }, JsonOptions);
    }
}
