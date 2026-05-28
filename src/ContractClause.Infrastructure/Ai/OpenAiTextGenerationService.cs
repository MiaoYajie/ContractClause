using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractClause.Infrastructure.Ai;

public class OpenAiTextGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    ILogger<OpenAiTextGenerationService> logger) : ITextGenerationService
{
    private readonly AiModelOptions _model = options.Value.TextModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_model.ApiKey);

    public async Task<string?> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var client = httpClientFactory.CreateClient("openai-text");
        var request = new
        {
            model = _model.ModelId,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" }
        };

        using var response = await client.PostAsJsonAsync("chat/completions", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("文本模型调用失败: {Status} {Body}", response.StatusCode, body);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return payload?.Choices.FirstOrDefault()?.Message.Content;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice> Choices { get; set; } = [];
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage Message { get; set; } = new();
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
