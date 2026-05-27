using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Import;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ContractClause.Application.Templates.Commands.ImportTemplate;

public class ImportTemplateCommandHandler(
    IServiceScopeFactory scopeFactory,
    IImportTaskRepository importTasks) : IRequestHandler<ImportTemplateCommand, ImportTemplateResult>
{
    public async Task<ImportTemplateResult> Handle(ImportTemplateCommand request, CancellationToken ct)
    {
        var task = new ImportTask
        {
            Id = Guid.NewGuid(),
            Status = "processing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await importTasks.AddAsync(task, ct);
        await importTasks.SaveChangesAsync(ct);

        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await ProcessImportAsync(scope.ServiceProvider, task.Id, request);
        }, CancellationToken.None);

        return new ImportTemplateResult(task.Id, "processing", "模板导入任务已提交，正在后台处理");
    }

    private static async Task ProcessImportAsync(IServiceProvider sp, Guid taskId, ImportTemplateCommand request)
    {
        var importRepo = sp.GetRequiredService<IImportTaskRepository>();
        var importer = sp.GetRequiredService<ITemplateContentImporter>();

        var task = await importRepo.GetByIdAsync(taskId);
        if (task is null) return;

        try
        {
            var outcome = await importer.ImportOrUpdateAsync(new TemplateImportRequest(
                request.HtmlContent,
                request.Title,
                request.Type,
                request.Categories,
                request.Tags,
                request.IsOfficial,
                request.OwnerId), default);

            task.Status = "completed";
            task.TemplateId = outcome.TemplateId;
            task.ClausesImported = outcome.ClausesImported;
        }
        catch (Exception ex)
        {
            task.Status = "failed";
            task.Errors.Add(ex.Message);
        }

        task.UpdatedAt = DateTime.UtcNow;
        await importRepo.UpdateAsync(task);
        await importRepo.SaveChangesAsync();
    }
}
