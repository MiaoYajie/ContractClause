using System.Text.Json;
using ContractClause.Api.Services;
using ContractClause.Application.Common.Interfaces;

namespace ContractClause.Api.Middleware;

public class ApiKeyAuthMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> PublicPaths =
    [
        "/healthz",
        "/health",
        "/openapi",
        "/scalar"
    ];

    public async Task InvokeAsync(HttpContext context, IApiKeyRepository apiKeys, ApiUserContext userContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/v1/apikeys", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
        {
            await WriteErrorAsync(context, 401, "MISSING_API_KEY", "X-Api-Key 缺失");
            return;
        }

        var apiKey = await apiKeys.GetByKeyAsync(keyHeader!);
        if (apiKey is null)
        {
            await WriteErrorAsync(context, 401, "INVALID_API_KEY", "API Key 无效");
            return;
        }

        userContext.OwnerId = apiKey.OwnerId;
        userContext.OwnerType = apiKey.OwnerType;
        userContext.IsAuthenticated = true;

        await next(context);
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { code, message, details = new { } }));
    }
}
