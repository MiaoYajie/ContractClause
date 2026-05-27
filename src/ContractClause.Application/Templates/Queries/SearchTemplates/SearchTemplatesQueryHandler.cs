using ContractClause.Application.Common;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Templates;
using MediatR;

namespace ContractClause.Application.Templates.Queries.SearchTemplates;

public class SearchTemplatesQueryHandler(
    ITemplateRepository templates,
    IVectorStore vectorStore,
    IEmbeddingService embedding,
    IUserContext userContext) : IRequestHandler<SearchTemplatesQuery, PagedTemplateSearchResultDto>
{
    public async Task<PagedTemplateSearchResultDto> Handle(SearchTemplatesQuery request, CancellationToken ct)
    {
        var ownerId = userContext.OwnerId;
        var skip = (request.Page - 1) * request.PageSize;
        var take = Math.Min(request.PageSize, 50);

        var keywordList = await templates.SearchKeywordAsync(
            request.Q, request.Type, request.Categories, request.IsOfficial, ownerId, 0, 200, ct);

        var vectorAvailable = await vectorStore.IsAvailableAsync(ct);
        float[]? queryVector = null;
        if (vectorAvailable && embedding.IsConfigured)
            queryVector = await embedding.EmbedAsync(request.Q, ct);

        string mode;
        List<(Template Template, double Score)> ranked;

        if (queryVector is not null && keywordList.Count > 0)
        {
            var vectorHits = await vectorStore.SearchTemplatesAsync(queryVector, 200, ct);
            var keywordIds = keywordList.Select(t => t.Id).ToList();
            var vectorIds = vectorHits.Select(h => h.Id).ToList();
            var fused = ReciprocalRankFusion.Fuse([keywordIds, vectorIds], take + skip);
            var templateMap = keywordList.ToDictionary(t => t.Id);
            foreach (var hit in vectorHits)
            {
                if (!templateMap.ContainsKey(hit.Id))
                {
                    var t = await templates.GetByIdAsync(hit.Id, ct);
                    if (t is not null) templateMap[hit.Id] = t;
                }
            }
            ranked = fused.Skip(skip).Take(take)
                .Select(f => (templateMap[f.Id], f.Score))
                .Where(x => x.Item1 is not null)
                .Select(x => (x.Item1!, x.Score))
                .ToList();
            mode = SearchMode.Hybrid;
        }
        else if (queryVector is not null)
        {
            var vectorHits = await vectorStore.SearchTemplatesAsync(queryVector, take + skip, ct);
            ranked = [];
            foreach (var hit in vectorHits.Skip(skip).Take(take))
            {
                var t = await templates.GetByIdAsync(hit.Id, ct);
                if (t is not null) ranked.Add((t, hit.Score));
            }
            mode = SearchMode.VectorOnly;
        }
        else if (keywordList.Count > 0)
        {
            ranked = keywordList.Skip(skip).Take(take).Select((t, i) => (t, 1.0 / (i + 1))).ToList();
            mode = SearchMode.KeywordOnly;
        }
        else
        {
            return new PagedTemplateSearchResultDto(0, request.Page, take, SearchMode.KeywordOnly, []);
        }

        var total = await templates.CountKeywordAsync(request.Q, request.Type, request.Categories, request.IsOfficial, ownerId, ct);
        var items = ranked.Select(x => new TemplateSearchItemDto(
            x.Template.Id, x.Template.Number, x.Template.Title, x.Template.Type,
            x.Template.Categories, x.Template.Tags, x.Template.Summary,
            x.Template.IsOfficial, x.Score)).ToList();

        return new PagedTemplateSearchResultDto(total, request.Page, take, mode, items);
    }
}
