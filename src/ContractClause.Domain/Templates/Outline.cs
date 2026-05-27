namespace ContractClause.Domain.Templates;

public class Outline
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string OutlineJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Template? Template { get; set; }
}
