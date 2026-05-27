namespace ContractClause.Mcp.Options;

public class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>传输方式：stdio（本地进程）或 http-sse（远程 HTTP / Streamable HTTP）</summary>
    public string Transport { get; set; } = "stdio";

    /// <summary>HTTP 模式监听端口</summary>
    public int HttpPort { get; set; } = 5001;

    /// <summary>MCP HTTP 端点路径前缀，默认 /mcp</summary>
    public string HttpPath { get; set; } = "/mcp";

    public bool IsHttpTransport =>
        Transport.Equals("http-sse", StringComparison.OrdinalIgnoreCase) ||
        Transport.Equals("http", StringComparison.OrdinalIgnoreCase);
}
