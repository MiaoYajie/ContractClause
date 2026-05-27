namespace ContractClause.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; set; } = "PostgreSQL";
    public string ConnectionString { get; set; } = string.Empty;
}
