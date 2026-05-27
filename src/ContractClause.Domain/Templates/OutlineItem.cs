namespace ContractClause.Domain.Templates;

public class OutlineItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<string> Variables { get; set; } = [];
    public List<OutlineItem> Children { get; set; } = [];
}
