namespace ContractClause.Domain.Templates;

public class Template
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public string Scenarios { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    /// <summary>法天使等外部系统的模板 ID。</summary>
    public string? ExternalId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public bool IsOfficial { get; set; }
    public Guid? OwnerId { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public Outline? Outline { get; set; }
    public ICollection<Clauses.Clause> Clauses { get; set; } = [];
}
