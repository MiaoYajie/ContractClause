namespace ContractClause.Infrastructure.Options;

public class AiOptions
{
    public const string SectionName = "Ai";
    public AiModelOptions EmbeddingModel { get; set; } = new();
    public AiModelOptions TextModel { get; set; } = new();
}

public class AiModelOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "text-embedding-3-small";
    public int Dimensions { get; set; } = 1536;
}
