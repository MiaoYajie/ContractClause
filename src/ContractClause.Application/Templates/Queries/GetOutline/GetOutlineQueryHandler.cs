using System.Text.Json;
using ContractClause.Application.Common.DTOs;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Templates;
using MediatR;

namespace ContractClause.Application.Templates.Queries.GetOutline;

public class GetOutlineQueryHandler(ITemplateRepository templates) : IRequestHandler<GetOutlineQuery, OutlineResultDto?>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<OutlineResultDto?> Handle(GetOutlineQuery request, CancellationToken ct)
    {
        var template = await templates.GetByIdWithOutlineAsync(request.TemplateId, ct);
        if (template?.Outline is null) return null;

        var items = JsonSerializer.Deserialize<List<OutlineItem>>(template.Outline.OutlineJson, JsonOptions) ?? [];
        return new OutlineResultDto(template.Id, MapItems(items));
    }

    private static IReadOnlyList<OutlineItemDto> MapItems(IEnumerable<OutlineItem> items) =>
        items.Select(i => new OutlineItemDto(i.Id, i.Title, i.Level, i.Variables, MapItems(i.Children))).ToList();
}
