namespace ContractClause.Application.Options;

public class FatianshiTemplateSyncOptions
{
    public const string SectionName = "FatianshiTemplateSync";

    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://contentadmin.fatianshi.cn";
    /// <summary>同步间隔（小时），设计为每天一次，默认 24。</summary>
    public int IntervalHours { get; set; } = 24;
    public int PageSize { get; set; } = 50;
    public bool RecommendOnly { get; set; } = true;
    public string? ApiKey { get; set; }
}
