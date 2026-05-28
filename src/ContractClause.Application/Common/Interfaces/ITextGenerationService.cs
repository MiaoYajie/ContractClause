namespace ContractClause.Application.Common.Interfaces;

public interface ITextGenerationService
{
    bool IsConfigured { get; }
    Task<string?> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
