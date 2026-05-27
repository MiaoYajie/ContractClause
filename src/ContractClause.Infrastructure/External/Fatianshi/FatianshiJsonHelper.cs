using System.Globalization;
using System.Text.Json;

namespace ContractClause.Infrastructure.External.Fatianshi;

internal static class FatianshiJsonHelper
{
    private static readonly string[] ListContainerNames = ["items", "list", "templates", "data", "records", "results"];
    private static readonly string[] IdNames = ["id", "templateId", "template_id"];
    private static readonly string[] TitleNames = ["title", "name", "templateName", "template_name"];
    private static readonly string[] HtmlNames = ["html", "content", "body", "htmlContent", "html_content", "templateHtml"];
    private static readonly string[] UpdatedAtNames = ["updatedAt", "updated_at", "gmtModified", "gmt_modified", "modifyTime", "modify_time", "lastModified"];

    public static IReadOnlyList<JsonElement> ExtractListItems(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray().ToList();

        foreach (var name in ListContainerNames)
        {
            if (!TryGetProperty(root, name, out var node)) continue;
            if (node.ValueKind == JsonValueKind.Array)
                return node.EnumerateArray().ToList();
            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var inner in ListContainerNames)
                {
                    if (TryGetProperty(node, inner, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        return arr.EnumerateArray().ToList();
                }
            }
        }

        return [];
    }

    public static string? GetString(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(el, name, out var prop)) continue;
            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Number => prop.GetRawText(),
                _ => null
            };
        }
        return null;
    }

    public static string? GetHtml(JsonElement el) => GetString(el, HtmlNames);

    public static string? GetId(JsonElement el) => GetString(el, IdNames);

    public static string? GetTitle(JsonElement el) => GetString(el, TitleNames);

    public static DateTime? GetUpdatedAt(JsonElement el)
    {
        foreach (var name in UpdatedAtNames)
        {
            if (!TryGetProperty(el, name, out var prop)) continue;
            var parsed = ParseDateTime(prop);
            if (parsed.HasValue) return parsed;
        }
        return null;
    }

    public static IReadOnlyList<string> GetStringList(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(el, name, out var prop)) continue;
            if (prop.ValueKind == JsonValueKind.Array)
            {
                return prop.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.GetRawText())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToList();
            }
            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (string.IsNullOrWhiteSpace(s)) return [];
                return s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }
        return [];
    }

    private static DateTime? ParseDateTime(JsonElement prop) => prop.ValueKind switch
    {
        JsonValueKind.String when DateTime.TryParse(prop.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) => dt.ToUniversalTime(),
        JsonValueKind.Number when prop.TryGetInt64(out var unix) => unix > 1_000_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(unix).UtcDateTime
            : DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime,
        _ => null
    };

    private static bool TryGetProperty(JsonElement el, string name, out JsonElement value)
    {
        if (el.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (el.TryGetProperty(name, out value)) return true;

        foreach (var prop in el.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
