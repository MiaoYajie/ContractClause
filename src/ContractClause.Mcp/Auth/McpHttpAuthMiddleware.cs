using System.Text.Json;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Mcp.Services;

namespace ContractClause.Mcp.Auth;

/// <summary>
/// HTTP 远程模式：校验 X-Api-Key，将会话用户上下文写入 HttpContext.Items。
/// </summary>
public class McpHttpAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiKeyRepository apiKeys)
    {
        if (!context.Request.Path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
        {
            await WriteUnauthorizedAsync(context, "Missing X-Api-Key");
            return;
        }

        var apiKey = await apiKeys.GetByKeyAsync(keyHeader!);
        if (apiKey is null)
        {
            await WriteUnauthorizedAsync(context, "Invalid API Key");
            return;
        }

        context.Items[nameof(McpAuthState)] = new McpAuthState
        {
            OwnerId = apiKey.OwnerId,
            OwnerType = apiKey.OwnerType,
            IsAuthenticated = true
        };

        await next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
