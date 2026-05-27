namespace ContractClause.Domain.Clauses;

public class Clause
{
    public Guid Id { get; set; }
    public Guid? TemplateId { get; set; }
    public string? OutlineItemId { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = [];
    public string ClauseType { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public string? VectorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public Templates.Template? Template { get; set; }
}
