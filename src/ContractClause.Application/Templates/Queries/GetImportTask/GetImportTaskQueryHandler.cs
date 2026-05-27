using ContractClause.Application.Common.Interfaces;
using MediatR;

namespace ContractClause.Application.Templates.Queries.GetImportTask;

public class GetImportTaskQueryHandler(IImportTaskRepository importTasks)
    : IRequestHandler<GetImportTaskQuery, ImportTaskStatusDto?>
{
    public async Task<ImportTaskStatusDto?> Handle(GetImportTaskQuery request, CancellationToken ct)
    {
        var task = await importTasks.GetByIdAsync(request.TaskId, ct);
        if (task is null) return null;

        return new ImportTaskStatusDto(
            task.Id, task.Status, task.TemplateId, task.ClausesImported, task.Errors);
    }
}
