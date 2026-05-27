namespace ContractClause.Domain.Sync;

/// <summary>记录法天使模板同步游标（单行配置）。</summary>
public class TemplateSyncState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public DateTime? LastSyncedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public int LastRunProcessed { get; set; }
    public List<string> LastRunErrors { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}
