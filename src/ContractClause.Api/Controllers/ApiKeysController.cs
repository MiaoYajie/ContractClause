using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ContractClause.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractClause.Api.Controllers;

[ApiController]
[Route("api/v1/apikeys")]
[Authorize]
public class ApiKeysController(IApiKeyRepository apiKeys) : ControllerBase
{
    /// <summary>
    /// 获取当前用户（token sub）的 API Key 列表。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> List(CancellationToken ct)
    {
        if (!TryGetOwnerId(out var ownerId, out var error))
            return error!;

        var keys = await apiKeys.GetByOwnerIdAsync(ownerId, ct);
        return Ok(keys.Select(k => new
        {
            k.Id,
            k.OwnerId,
            k.OwnerType,
            k.CreatedAt,
            k.UpdatedAt,
            apiKeyPreview = MaskKey(k.Key)
        }));
    }

    /// <summary>
    /// 使用 AuthServer 签发的 Bearer access token 创建 API Key；owner 取自 token 的 sub。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateApiKeyRequest? request, CancellationToken ct)
    {
        if (!TryGetOwnerId(out var ownerId, out var error))
            return error!;

        var key = await apiKeys.CreateAsync(ownerId, request?.OwnerType ?? "User", ct);
        return Created($"/api/v1/apikeys/{key.Id}", new { key.Id, apiKey = key.Key, key.OwnerId, key.OwnerType });
    }

    /// <summary>
    /// 删除当前用户名下指定的 API Key（软删除）。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetOwnerId(out var ownerId, out var error))
            return error!;

        var deleted = await apiKeys.SoftDeleteAsync(id, ownerId, ct);
        return deleted ? NoContent() : NotFound(new { code = "NOT_FOUND", message = "API Key 不存在或无权删除" });
    }

    private bool TryGetOwnerId(out Guid ownerId, out ActionResult? error)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out ownerId))
        {
            ownerId = default;
            error = BadRequest(new
            {
                code = "INVALID_SUB",
                message = "无法从 access token 的 sub 声明解析有效的用户 ID（Guid）"
            });
            return false;
        }

        error = null;
        return true;
    }

    private static string MaskKey(string key) =>
        key.Length <= 8 ? "****" : $"{key[..7]}...{key[^4..]}";
}

public record CreateApiKeyRequest(string? OwnerType);
