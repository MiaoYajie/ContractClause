using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ContractClause.Infrastructure.Ai;

public class OpenAiEmbeddingService(IHttpClientFactory httpClientFactory, IOptions<AiOptions> options)
    : IEmbeddingService
{
    private readonly AiModelOptions _model = options.Value.EmbeddingModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_model.ApiKey);

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var client = httpClientFactory.CreateClient("openai");
        var request = new
        {
            model = _model.ModelId,
            input = text,
            dimensions = _model.Dimensions
        };

        using var response = await client.PostAsJsonAsync("embeddings", request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return body?.Data.FirstOrDefault()?.Embedding;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = [];
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
