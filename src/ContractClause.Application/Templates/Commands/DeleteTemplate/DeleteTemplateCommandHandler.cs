using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Templates.Commands.DeleteTemplate;

public class DeleteTemplateCommandHandler(
    ITemplateRepository templates,
    IClauseRepository clauses) : IRequestHandler<DeleteTemplateCommand, bool>
{
    public async Task<bool> Handle(DeleteTemplateCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(request.Id, ct);
        if (template is null) return false;

        await templates.SoftDeleteAsync(request.Id, ct);
        await clauses.SoftDeleteByTemplateAsync(request.Id, ct);
        await templates.SaveChangesAsync(ct);
        await clauses.SaveChangesAsync(ct);
        return true;
    }
}
