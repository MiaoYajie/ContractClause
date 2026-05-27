using ContractClause.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContractClause.Api.Controllers;

[ApiController]
[Route("api/v1/apikeys")]
public class ApiKeysController(IApiKeyRepository apiKeys) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateApiKeyRequest request, CancellationToken ct)
    {
        var key = await apiKeys.CreateAsync(request.OwnerId, request.OwnerType ?? "User", ct);
        return Created($"/api/v1/apikeys/{key.Id}", new { key.Id, apiKey = key.Key, key.OwnerId, key.OwnerType });
    }
}

public record CreateApiKeyRequest(Guid OwnerId, string? OwnerType);
