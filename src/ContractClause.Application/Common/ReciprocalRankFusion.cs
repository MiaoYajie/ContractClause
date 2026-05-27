namespace ContractClause.Application.Common;

public static class ReciprocalRankFusion
{
    private const int K = 60;

    public static IReadOnlyList<(Guid Id, double Score)> Fuse(
        IReadOnlyList<IReadOnlyList<Guid>> rankedLists,
        int take)
    {
        var scores = new Dictionary<Guid, double>();
        foreach (var list in rankedLists)
        {
            for (var rank = 0; rank < list.Count; rank++)
            {
                var id = list[rank];
                scores.TryGetValue(id, out var current);
                scores[id] = current + 1.0 / (K + rank + 1);
            }
        }

        return scores
            .OrderByDescending(x => x.Value)
            .Take(take)
            .Select(x => (x.Key, x.Value))
            .ToList();
    }
}
