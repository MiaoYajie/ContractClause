namespace ContractClause.Infrastructure.Options;

public class VectorStoreOptions
{
    public const string SectionName = "VectorStore";
    public string Provider { get; set; } = "Qdrant";
    public QdrantOptions Qdrant { get; set; } = new();
}

public class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
}
