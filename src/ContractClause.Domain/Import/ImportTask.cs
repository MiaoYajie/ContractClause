namespace ContractClause.Domain.Import;

public class ImportTask
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "processing";
    public Guid? TemplateId { get; set; }
    public int ClausesImported { get; set; }
    public List<string> Errors { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
