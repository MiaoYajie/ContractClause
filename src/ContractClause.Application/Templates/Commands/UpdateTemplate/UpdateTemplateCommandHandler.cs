using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Templates.Commands.UpdateTemplate;

public class UpdateTemplateCommandHandler(
    ITemplateRepository templates,
    IVectorStore vectorStore,
    IEmbeddingService embedding) : IRequestHandler<UpdateTemplateCommand, bool>
{
    public async Task<bool> Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(request.Id, ct);
        if (template is null) return false;

        if (request.Title is not null) template.Title = request.Title;
        if (request.Type is not null) template.Type = request.Type;
        if (request.Categories is not null) template.Categories = request.Categories.ToList();
        if (request.Tags is not null) template.Tags = request.Tags.ToList();
        if (request.Summary is not null) template.Summary = request.Summary;
        if (request.Scenarios is not null) template.Scenarios = request.Scenarios;
        template.UpdatedAt = DateTime.UtcNow;
        template.Version++;

        await templates.UpdateAsync(template, ct);
        await templates.SaveChangesAsync(ct);

        if (await vectorStore.IsAvailableAsync(ct) && embedding.IsConfigured)
        {
            var text = $"{template.Title}\n{template.Summary}\n{template.Scenarios}";
            var vector = await embedding.EmbedAsync(text, ct);
            if (vector is not null)
            {
                await vectorStore.UpsertTemplateAsync(template.Id, vector, new Dictionary<string, object>
                {
                    ["title"] = template.Title,
                    ["type"] = template.Type,
                    ["categories"] = template.Categories,
                    ["isOfficial"] = template.IsOfficial
                }, ct);
            }
        }

        return true;
    }
}
