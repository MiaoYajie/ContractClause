using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Clauses;
using MediatR;

namespace ContractClause.Application.Clauses.Queries.GetClauses;

public class GetClausesQueryHandler(
    IClauseRepository clauses,
    IVectorStore vectorStore,
    IEmbeddingService embedding) : IRequestHandler<GetClausesQuery, PagedClauseSearchResultDto>
{
    public async Task<PagedClauseSearchResultDto> Handle(GetClausesQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var take = Math.Min(request.PageSize, 50);

        if (!string.IsNullOrWhiteSpace(request.OutlineItemId) && request.TemplateId.HasValue)
        {
            var list = await clauses.GetByTemplateAndOutlineItemAsync(
                request.TemplateId.Value, request.OutlineItemId, request.ClauseType, skip, take, ct);
            return new PagedClauseSearchResultDto(list.Count, Map(list, 1.0));
        }

        var q = request.Q ?? string.Empty;
        if (string.IsNullOrWhiteSpace(q))
        {
            if (!request.TemplateId.HasValue)
                return new PagedClauseSearchResultDto(0, []);
            var list = await clauses.GetByTemplateAndOutlineItemAsync(
                request.TemplateId.Value, null, request.ClauseType, skip, take, ct);
            return new PagedClauseSearchResultDto(list.Count, Map(list, 1.0));
        }

        var keywordList = await clauses.SearchKeywordAsync(q, request.TemplateId, request.ClauseType, 0, 200, ct);

        var vectorAvailable = await vectorStore.IsAvailableAsync(ct);
        float[]? queryVector = null;
        if (vectorAvailable && embedding.IsConfigured)
            queryVector = await embedding.EmbedAsync(q, ct);

        string mode;
        List<(Clause Clause, double Score)> ranked;

        if (queryVector is not null && keywordList.Count > 0)
        {
            var vectorHits = await vectorStore.SearchClausesAsync(queryVector, request.TemplateId, request.ClauseType, 200, ct);
            var keywordIds = keywordList.Select(c => c.Id).ToList();
            var vectorIds = vectorHits.Select(h => h.Id).ToList();
            var fused = ReciprocalRankFusion.Fuse([keywordIds, vectorIds], take + skip);
            var map = keywordList.ToDictionary(c => c.Id);
            foreach (var hit in vectorHits)
            {
                if (!map.ContainsKey(hit.Id))
                {
                    var c = await clauses.GetByIdAsync(hit.Id, ct);
                    if (c is not null) map[hit.Id] = c;
                }
            }
            ranked = fused.Skip(skip).Take(take)
                .Select(f => (map.GetValueOrDefault(f.Id)!, f.Score))
                .Where(x => x.Item1 is not null)
                .ToList()!;
            mode = SearchMode.Hybrid;
        }
        else if (queryVector is not null)
        {
            var vectorHits = await vectorStore.SearchClausesAsync(queryVector, request.TemplateId, request.ClauseType, take + skip, ct);
            ranked = [];
            foreach (var hit in vectorHits.Skip(skip).Take(take))
            {
                var c = await clauses.GetByIdAsync(hit.Id, ct);
                if (c is not null) ranked.Add((c, hit.Score));
            }
            mode = SearchMode.VectorOnly;
        }
        else
        {
            ranked = keywordList.Skip(skip).Take(take).Select((c, i) => (c, 1.0 / (i + 1))).ToList();
            mode = SearchMode.KeywordOnly;
        }

        var total = await clauses.CountKeywordAsync(q, request.TemplateId, request.ClauseType, ct);
        return new PagedClauseSearchResultDto(total, Map(ranked.Select(r => r.Clause), ranked.Select(r => r.Score)), mode);
    }

    private static IReadOnlyList<ClauseItemDto> Map(IEnumerable<Clause> list, double score) =>
        list.Select(c => new ClauseItemDto(c.Id, c.TemplateId, c.OutlineItemId, c.ClauseType, c.Text, c.Variables, c.Keywords, score)).ToList();

    private static IReadOnlyList<ClauseItemDto> Map(IEnumerable<Clause> list, IEnumerable<double> scores)
    {
        return list.Zip(scores, (c, s) => new ClauseItemDto(c.Id, c.TemplateId, c.OutlineItemId, c.ClauseType, c.Text, c.Variables, c.Keywords, s)).ToList();
    }
}
