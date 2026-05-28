using System.Text.Json;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Clauses;
using ContractClause.Domain.Templates;
using Microsoft.Extensions.Logging;

namespace ContractClause.Application.Templates.Processing;

public class TemplateContentProcessingService(
    ITemplateSourceBlobReader sourceBlob,
    ITemplateProcessedBlobWriter processedBlob,
    ITemplateRepository templates,
    IClauseRepository clauses,
    IVectorStore vectorStore,
    IEmbeddingService embedding,
    ITextGenerationService textGeneration,
    ILogger<TemplateContentProcessingService> logger) : ITemplateContentProcessingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<TemplateContentProcessingResult> ProcessAsync(Guid templateId, int version, CancellationToken ct = default)
    {
        if (!sourceBlob.IsConfigured)
        {
            logger.LogWarning("模板 Blob 未配置，跳过内容处理: {TemplateId}", templateId);
            return new TemplateContentProcessingResult(false, 0, "TemplateBlob 未配置");
        }

        try
        {
            var html = await sourceBlob.ReadTemplateHtmlAsync(templateId, version, ct);
            if (string.IsNullOrWhiteSpace(html))
            {
                logger.LogWarning("未读取到模板 HTML: {TemplateId} v{Version}", templateId, version);
                return new TemplateContentProcessingResult(false, 0, "模板 HTML 不存在");
            }

            var now = DateTime.UtcNow;
            var processed = TemplateHtmlProcessor.Process(html, templateId, now);

            if (processedBlob.IsConfigured)
            {
                var questionsJson = await BuildQuestionsJsonAsync(processed.QuestionContext, ct);
                await processedBlob.WriteProcessedFilesAsync(templateId, new Dictionary<string, string>
                {
                    ["default.html"] = processed.DefaultHtml,
                    ["default-pure.html"] = processed.DefaultPureHtml,
                    ["default.md"] = processed.DefaultMarkdown,
                    ["default.txt"] = processed.DefaultPlainText,
                    ["question.json"] = questionsJson
                }, ct);
            }

            var template = await templates.GetByIdWithOutlineAsync(templateId, ct);
            if (template is null)
            {
                logger.LogWarning("模板不存在，无法写入大纲与条款: {TemplateId}", templateId);
                return new TemplateContentProcessingResult(false, 0, "模板不存在");
            }

            var outline = template.Outline ?? new Outline
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                CreatedAt = now
            };
            outline.OutlineJson = TemplateContentProcessor.SerializeOutline(processed.Outline);
            outline.UpdatedAt = now;
            template.Outline = outline;
            template.UpdatedAt = now;

            await clauses.SoftDeleteByTemplateAsync(templateId, ct);
            await clauses.AddRangeAsync(processed.Clauses, ct);
            await templates.UpdateAsync(template, ct);
            await templates.SaveChangesAsync(ct);
            await clauses.SaveChangesAsync(ct);

            await UpsertVectorsAsync(template, processed.Clauses, ct);

            logger.LogInformation(
                "模板内容处理完成: {TemplateId} v{Version}, clauses={ClauseCount}",
                templateId, version, processed.Clauses.Count);

            return new TemplateContentProcessingResult(true, processed.Clauses.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "模板内容处理失败: {TemplateId} v{Version}", templateId, version);
            return new TemplateContentProcessingResult(false, 0, ex.Message);
        }
    }

    private async Task<string> BuildQuestionsJsonAsync(TemplateHtmlProcessor.QuestionContext context, CancellationToken ct)
    {
        var payload = new
        {
            outline = context.Outline,
            articles = context.Articles,
            termGroups = context.TermGroups,
            variables = context.Variables
        };
        var inputJson = JsonSerializer.Serialize(payload, JsonOptions);

        if (textGeneration.IsConfigured)
        {
            const string systemPrompt = """
                你是合同模板助手。根据提供的模板大纲、文本说明、条款组说明和变量信息，生成一份用于向用户收集信息的问答列表。
                只输出 JSON，格式为 {"questions":[{"id":"1","question":"...","category":"article|termGroup|variable|outline","relatedKey":"..."}]}。
                问题应简洁、面向业务用户，避免重复。
                """;

            var generated = await textGeneration.GenerateJsonAsync(
                systemPrompt,
                $"请根据以下模板信息生成问答列表：\n{inputJson}",
                ct);

            if (!string.IsNullOrWhiteSpace(generated))
                return generated;
        }

        var fallbackQuestions = BuildFallbackQuestions(context);
        return JsonSerializer.Serialize(new { questions = fallbackQuestions }, JsonOptions);
    }

    private static List<object> BuildFallbackQuestions(TemplateHtmlProcessor.QuestionContext context)
    {
        var questions = new List<object>();
        var id = 1;

        foreach (var variable in context.Variables)
        {
            questions.Add(new
            {
                id = id++.ToString(),
                question = string.IsNullOrWhiteSpace(variable.Description)
                    ? $"请填写变量「{variable.Name}」"
                    : variable.Description,
                category = "variable",
                relatedKey = variable.Name
            });
        }

        foreach (var article in context.Articles)
        {
            if (string.IsNullOrWhiteSpace(article.Description))
                continue;

            questions.Add(new
            {
                id = id++.ToString(),
                question = article.Description,
                category = "article",
                relatedKey = article.Title
            });
        }

        foreach (var group in context.TermGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Description))
                continue;

            questions.Add(new
            {
                id = id++.ToString(),
                question = group.Description,
                category = "termGroup",
                relatedKey = group.Title
            });
        }

        return questions;
    }

    private async Task UpsertVectorsAsync(Template template, List<Clause> clauseEntities, CancellationToken ct)
    {
        if (!await vectorStore.IsAvailableAsync(ct) || !embedding.IsConfigured)
            return;

        await vectorStore.EnsureCollectionsAsync(ct);

        foreach (var clause in clauseEntities)
        {
            var text = HtmlToPlain(clause.Text);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var snippet = text.Length > 2000 ? text[..2000] : text;
            var cVec = await embedding.EmbedAsync(snippet, ct);
            if (cVec is null) continue;

            await vectorStore.UpsertClauseAsync(clause.Id, cVec, new Dictionary<string, object>
            {
                ["templateId"] = template.Id.ToString(),
                ["clauseType"] = clause.ClauseType,
                ["keywords"] = clause.Keywords
            }, ct);
            clause.VectorId = clause.Id.ToString();
        }

        await clauses.SaveChangesAsync(ct);
    }

    private static string HtmlToPlain(string html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : TemplateHtmlProcessor.HtmlToPlainText(html);
}
