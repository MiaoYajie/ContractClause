using System.Text.Json;
using System.Text.RegularExpressions;
using ContractClause.Application.Common;
using ContractClause.Domain.Clauses;
using ContractClause.Domain.Templates;

namespace ContractClause.Application.Templates;

public static partial class TemplateContentProcessor
{
    public static string HtmlToMarkdown(string html)
    {
        var text = HtmlTag().Replace(html, "\n");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
    }

    public static List<OutlineItem> ExtractOutlineFromMarkdown(string markdown)
    {
        var items = new List<OutlineItem>();
        var stack = new Stack<(OutlineItem Item, int Level)>();
        var lines = markdown.Split('\n');
        var counter = 0;

        foreach (var line in lines)
        {
            var m = HeadingLine().Match(line.Trim());
            if (!m.Success) continue;

            var level = m.Groups[1].Value.Length;
            var title = m.Groups[2].Value.Trim();
            counter++;
            var item = new OutlineItem
            {
                Id = counter.ToString(),
                Title = title,
                Level = level,
                Variables = VariableHelper.Extract(title)
            };

            while (stack.Count > 0 && stack.Peek().Level >= level)
                stack.Pop();

            if (stack.Count == 0)
                items.Add(item);
            else
                stack.Peek().Item.Children.Add(item);

            stack.Push((item, level));
        }

        if (items.Count == 0)
            items.Add(new OutlineItem { Id = "1", Title = "正文", Level = 1 });

        return items;
    }

    public static List<Clause> SegmentClauses(string markdown, List<OutlineItem> outline, Guid templateId, DateTime now)
    {
        var flat = FlattenOutline(outline);
        var clauses = new List<Clause>();
        var sections = markdown.Split("\n## ", StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < sections.Length; i++)
        {
            var text = i == 0 ? sections[i] : "## " + sections[i];
            if (string.IsNullOrWhiteSpace(text)) continue;
            var outlineItem = i < flat.Count ? flat[i] : flat.LastOrDefault();
            clauses.Add(new Clause
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                OutlineItemId = outlineItem?.Id,
                Text = text.Trim(),
                Variables = VariableHelper.Extract(text),
                ClauseType = "正文",
                Keywords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(10).ToList(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (clauses.Count == 0)
        {
            clauses.Add(new Clause
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                OutlineItemId = "1",
                Text = markdown,
                Variables = VariableHelper.Extract(markdown),
                ClauseType = "正文",
                Keywords = [],
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        return clauses;
    }

    public static string SerializeOutline(List<OutlineItem> items) =>
        JsonSerializer.Serialize(items);

    private static List<OutlineItem> FlattenOutline(IEnumerable<OutlineItem> items)
    {
        var result = new List<OutlineItem>();
        foreach (var item in items)
        {
            result.Add(item);
            result.AddRange(FlattenOutline(item.Children));
        }
        return result;
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingLine();
}
