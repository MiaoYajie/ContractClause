namespace ContractClause.Application.Options;

public class FatianshiTemplateSyncOptions
{
    public const string SectionName = "FatianshiTemplateSync";

    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://tsapi.fatianshi.cn";
    public int IntervalHours { get; set; } = 4;
    public int PageSize { get; set; } = 50;
    public string SearchUpdatedAfterParameter { get; set; } = "updatedAfter";
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; } = "Authorization";
    public bool MarkAsOfficial { get; set; } = true;
    public string DefaultType { get; set; } = "合同";
}
