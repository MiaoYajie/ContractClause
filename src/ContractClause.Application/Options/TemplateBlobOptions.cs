namespace ContractClause.Application.Options;

public class TemplateBlobOptions
{
    public const string SectionName = "TemplateBlob";

    public string ConnectionString { get; set; } = string.Empty;
    public string SourceContainerName { get; set; } = "template";
    public string OutputContainerName { get; set; } = "contractclause";
    public string SourceBlobPathFormat { get; set; } = "{templateId}/{version}";
    public string OutputBlobDirectoryFormat { get; set; } = "template/{templateId}";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString)
        && !string.IsNullOrWhiteSpace(SourceContainerName)
        && !string.IsNullOrWhiteSpace(OutputContainerName);
}
