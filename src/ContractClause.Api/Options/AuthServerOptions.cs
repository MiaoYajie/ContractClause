namespace ContractClause.Api.Options;

public class AuthServerOptions
{
    public const string SectionName = "AuthServer";

    public string Authority { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string ApiSecret { get; set; } = string.Empty;
}
