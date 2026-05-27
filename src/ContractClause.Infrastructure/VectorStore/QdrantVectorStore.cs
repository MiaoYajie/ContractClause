using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ContractClause.Infrastructure.VectorStore;

public class QdrantVectorStore(IOptions<VectorStoreOptions> options, ILogger<QdrantVectorStore> logger)
    : IVectorStore
{
    private const string TemplateCollection = "templates";
    private const string ClauseCollection = "clauses";

    private QdrantClient? _client;
    private readonly QdrantOptions _options = options.Value.Qdrant;

    private QdrantClient Client => _client ??= new QdrantClient(new Uri(_options.Endpoint), apiKey: string.IsNullOrEmpty(_options.ApiKey) ? null : _options.ApiKey);

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            await Client.ListCollectionsAsync(cancellationToken: ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Qdrant unavailable");
            return false;
        }
    }

    public async Task EnsureCollectionsAsync(CancellationToken ct = default)
    {
        await EnsureCollectionAsync(TemplateCollection, ct);
        await EnsureCollectionAsync(ClauseCollection, ct);
    }

    private async Task EnsureCollectionAsync(string name, CancellationToken ct)
    {
        var collections = await Client.ListCollectionsAsync(cancellationToken: ct);
        if (collections.Contains(name)) return;

        await Client.CreateCollectionAsync(name, new VectorParams { Size = 1536, Distance = Distance.Cosine }, cancellationToken: ct);
    }

    public async Task UpsertTemplateAsync(Guid id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default)
    {
        await EnsureCollectionAsync(TemplateCollection, ct);
        await UpsertPointAsync(TemplateCollection, id, vector, payload, ct);
    }

    public async Task UpsertClauseAsync(Guid id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default)
    {
        await EnsureCollectionAsync(ClauseCollection, ct);
        await UpsertPointAsync(ClauseCollection, id, vector, payload, ct);
    }

    private async Task UpsertPointAsync(string collection, Guid id, float[] vector, IDictionary<string, object> payload, CancellationToken ct)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = id.ToString() },
            Vectors = vector
        };
        foreach (var (key, value) in payload)
        {
            point.Payload[key] = ValueFromObject(value);
        }

        await Client.UpsertAsync(collection, [point], cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorSearchHit>> SearchTemplatesAsync(float[] queryVector, int limit, CancellationToken ct = default)
    {
        if (!await IsAvailableAsync(ct)) return [];
        return await SearchAsync(TemplateCollection, queryVector, limit, null, null, ct);
    }

    public async Task<IReadOnlyList<VectorSearchHit>> SearchClausesAsync(
        float[] queryVector, Guid? templateId, string? clauseType, int limit, CancellationToken ct = default)
    {
        if (!await IsAvailableAsync(ct)) return [];
        return await SearchAsync(ClauseCollection, queryVector, limit, templateId, clauseType, ct);
    }

    private async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        string collection, float[] queryVector, int limit, Guid? templateId, string? clauseType, CancellationToken ct)
    {
        try
        {
            var results = await Client.SearchAsync(
                collectionName: collection,
                vector: queryVector,
                limit: (ulong)limit,
                cancellationToken: ct);
            return results
                .Where(r => r.Id.HasUuid && Guid.TryParse(r.Id.Uuid, out _))
                .Select(r => new VectorSearchHit(Guid.Parse(r.Id.Uuid), r.Score))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vector search failed for {Collection}", collection);
            return [];
        }
    }

    public Task DeleteTemplateAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

    public Task DeleteClauseAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

    private static Value ValueFromObject(object value) => value switch
    {
        string s => s,
        bool b => b,
        int i => i,
        long l => l,
        double d => d,
        IEnumerable<string> list => new Value { StringValue = string.Join(",", list) },
        _ => value.ToString() ?? string.Empty
    };
}
