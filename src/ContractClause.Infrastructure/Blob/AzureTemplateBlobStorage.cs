using Azure.Storage.Blobs;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractClause.Infrastructure.Blob;

public class AzureTemplateBlobStorage(
    IOptions<TemplateBlobOptions> options,
    ILogger<AzureTemplateBlobStorage> logger) : ITemplateSourceBlobReader, ITemplateProcessedBlobWriter
{
    private readonly TemplateBlobOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string?> ReadTemplateHtmlAsync(Guid templateId, int version, CancellationToken ct = default)
    {
        var container = CreateSourceContainerClient();
        if (container is null) return null;

        var blobName = FormatPath(_options.SourceBlobPathFormat, templateId, version, null);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(ct))
        {
            logger.LogWarning(
                "模板源 Blob 不存在: {Container}/{BlobName}",
                _options.SourceContainerName,
                blobName);
            return null;
        }

        var download = await blob.DownloadContentAsync(ct);
        return download.Value.Content.ToString();
    }

    public async Task WriteProcessedFilesAsync(
        Guid templateId,
        IReadOnlyDictionary<string, string> files,
        CancellationToken ct = default)
    {
        var container = CreateOutputContainerClient();
        if (container is null) return;

        var directory = FormatPath(_options.OutputBlobDirectoryFormat, templateId, version: null, fileName: null).TrimEnd('/');

        foreach (var (fileName, content) in files)
        {
            var blobName = $"{directory}/{fileName}";
            var blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(BinaryData.FromString(content), overwrite: true, cancellationToken: ct);
            logger.LogDebug(
                "已写入处理产物: {Container}/{BlobName}",
                _options.OutputContainerName,
                blobName);
        }
    }

    private BlobServiceClient? CreateServiceClient()
    {
        if (!IsConfigured)
            return null;

        return new BlobServiceClient(_options.ConnectionString);
    }

    private BlobContainerClient? CreateSourceContainerClient()
    {
        var service = CreateServiceClient();
        return service?.GetBlobContainerClient(_options.SourceContainerName);
    }

    private BlobContainerClient? CreateOutputContainerClient()
    {
        var service = CreateServiceClient();
        return service?.GetBlobContainerClient(_options.OutputContainerName);
    }

    private static string FormatPath(string format, Guid templateId, int? version, string? fileName) =>
        format
            .Replace("{templateId}", templateId.ToString("D"), StringComparison.OrdinalIgnoreCase)
            .Replace("{id}", templateId.ToString("D"), StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", version?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{fileName}", fileName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
}
