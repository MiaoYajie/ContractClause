using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using ContractClause.Application.Common;
using ContractClause.Domain.Clauses;
using ContractClause.Domain.Templates;

namespace ContractClause.Application.Templates.Processing;

public static partial class TemplateHtmlProcessor
{
    private const string TermBlockClass = "fts-term-block";
    private const string TermGroupClass = "fts-term-group-block";

    public record VariableInfo(string Name, string Description, string DefaultValue);

    public record TextSectionInfo(string Title, string Description);

    public record QuestionContext(
        IReadOnlyList<OutlineItem> Outline,
        IReadOnlyList<TextSectionInfo> Articles,
        IReadOnlyList<TextSectionInfo> TermGroups,
        IReadOnlyList<VariableInfo> Variables);

    public record ProcessResult(
        string DefaultHtml,
        string DefaultPureHtml,
        string DefaultMarkdown,
        string DefaultPlainText,
        List<OutlineItem> Outline,
        List<Clause> Clauses,
        QuestionContext QuestionContext);

    public static ProcessResult Process(string html, Guid templateId, DateTime now)
    {
        var document = ParseDocument(html);
        var defaultDoc = BuildDefaultDocument(document);
        var defaultHtml = defaultDoc.DocumentElement?.OuterHtml ?? string.Empty;
        var defaultPureHtml = StripAttributes(defaultHtml);
        var defaultMarkdown = HtmlToMarkdown(defaultPureHtml);
        var defaultPlainText = HtmlToPlainText(defaultPureHtml);

        var outline = ExtractOutline(document);
        var clauses = SegmentClauses(document, outline, templateId, now);
        var questionContext = BuildQuestionContext(document, outline);

        return new ProcessResult(
            defaultHtml,
            defaultPureHtml,
            defaultMarkdown,
            defaultPlainText,
            outline,
            clauses,
            questionContext);
    }

    private static IDocument ParseDocument(string html)
    {
        var parser = new HtmlParser();
        return parser.ParseDocument(html);
    }

    private static IDocument BuildDefaultDocument(IDocument source)
    {
        var clone = ParseDocument(source.DocumentElement?.OuterHtml ?? source.Body?.InnerHtml ?? string.Empty);

        foreach (var bundle in clone.QuerySelectorAll("fts-bundle"))
        {
            foreach (var article in bundle.QuerySelectorAll(":scope > article").ToList())
            {
                if (!IsNecessaryArticle(article))
                    article.Remove();
            }
        }

        foreach (var termBlock in clone.QuerySelectorAll($"div.{TermBlockClass}").ToList())
        {
            if (!IsCheckedTerm(termBlock))
                termBlock.Remove();
        }

        return clone;
    }

    private static bool IsNecessaryArticle(IElement article)
    {
        var value = article.GetAttribute("data-is-necessary");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCheckedTerm(IElement termBlock)
    {
        var value = termBlock.GetAttribute("data-is-checked");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExclusiveSection(IElement section)
    {
        var value = section.GetAttribute("data-is-multiple");
        return value is null || value == "0";
    }

    private static bool ShouldIncludeTermInOutline(IElement termBlock)
    {
        var section = termBlock.Closest($"section.{TermGroupClass}");
        if (section is null)
            return true;

        if (!IsExclusiveSection(section))
            return true;

        return IsCheckedTerm(termBlock);
    }

    public static string StripAttributes(string html)
    {
        var document = ParseDocument(html);
        foreach (var element in document.QuerySelectorAll("*"))
        {
            var attrs = element.Attributes.ToList();
            foreach (var attr in attrs)
                element.RemoveAttribute(attr.Name);
        }

        foreach (var style in document.QuerySelectorAll("style").ToList())
            style.Remove();

        return document.DocumentElement?.OuterHtml ?? html;
    }

    public static string HtmlToPlainText(string html)
    {
        var document = ParseDocument(html);
        return NormalizeWhitespace(document.Body?.TextContent ?? string.Empty);
    }

    public static string HtmlToMarkdown(string html)
    {
        var document = ParseDocument(html);
        var builder = new StringBuilder();
        if (document.Body is not null)
            AppendMarkdown(document.Body, builder, 0);
        return NormalizeWhitespace(builder.ToString());
    }

    private static void AppendMarkdown(IElement element, StringBuilder builder, int listDepth)
    {
        switch (element)
        {
            case IHtmlHeadingElement heading:
                builder.AppendLine($"{new string('#', heading.TagName.Length - 1)} {heading.TextContent.Trim()}");
                builder.AppendLine();
                return;
            case IHtmlParagraphElement:
                AppendInlineChildren(element, builder);
                builder.AppendLine();
                builder.AppendLine();
                return;
            case IHtmlUnorderedListElement:
                AppendListItems(element, builder, listDepth, ordered: false);
                builder.AppendLine();
                return;
            case IHtmlOrderedListElement:
                AppendListItems(element, builder, listDepth, ordered: true);
                builder.AppendLine();
                return;
            case IHtmlTableElement:
                AppendTable(element, builder);
                builder.AppendLine();
                return;
        }

        foreach (var child in element.Children)
            AppendMarkdown(child, builder, listDepth);
    }

    private static void AppendListItems(IElement list, StringBuilder builder, int listDepth, bool ordered)
    {
        var index = 1;
        foreach (var item in list.Children.Where(c => c.TagName.Equals("LI", StringComparison.OrdinalIgnoreCase)))
        {
            var indent = new string(' ', listDepth * 2);
            var marker = ordered ? $"{index}." : "-";
            builder.Append($"{indent}{marker} ");
            AppendInlineChildren(item, builder);
            builder.AppendLine();
            index++;
        }
    }

    private static void AppendTable(IElement table, StringBuilder builder)
    {
        foreach (var row in table.QuerySelectorAll("tr"))
        {
            var cells = row.QuerySelectorAll("th,td").Select(c => c.TextContent.Trim()).ToList();
            if (cells.Count == 0) continue;
            builder.AppendLine("| " + string.Join(" | ", cells) + " |");
        }
    }

    private static void AppendInlineChildren(IElement element, StringBuilder builder)
    {
        foreach (var node in element.ChildNodes)
        {
            if (node.NodeType == NodeType.Text)
                builder.Append(node.TextContent);
            else if (node is IElement child)
                builder.Append(child.TextContent);
        }
    }

    public static List<OutlineItem> ExtractOutline(IDocument document)
    {
        var items = new List<OutlineItem>();
        var counter = 0;

        foreach (var article in document.QuerySelectorAll("article"))
        {
            var articleTitle = GetArticleTitle(article);
            if (string.IsNullOrWhiteSpace(articleTitle))
                continue;

            counter++;
            var articleItem = CreateOutlineItem(counter, articleTitle, 1);
            items.Add(articleItem);

            var stack = new Stack<(OutlineItem Item, int Level)>();
            stack.Push((articleItem, 1));

            foreach (var node in article.Descendants().OfType<IElement>())
            {
                if (node.TagName is "H1" or "H2" or "H3" or "H4" or "H5")
                {
                    if (node.TagName == "H1" && node.Closest("article") == article && node.TextContent.Trim() == articleTitle)
                        continue;

                    if (node.Closest($"div.{TermBlockClass}") is { } termBlock && !ShouldIncludeTermInOutline(termBlock))
                        continue;

                    var level = node.TagName switch
                    {
                        "H1" => 1,
                        "H2" => 2,
                        "H3" => 3,
                        "H4" => 4,
                        _ => 5
                    };

                    counter++;
                    var item = CreateOutlineItem(counter, node.TextContent.Trim(), level);

                    while (stack.Count > 0 && stack.Peek().Level >= level)
                        stack.Pop();

                    if (stack.Count == 0)
                        articleItem.Children.Add(item);
                    else
                        stack.Peek().Item.Children.Add(item);

                    stack.Push((item, level));
                }
            }
        }

        if (items.Count == 0)
            items.Add(CreateOutlineItem(1, "正文", 1));

        return items;
    }

    private static OutlineItem CreateOutlineItem(int id, string title, int level) => new()
    {
        Id = id.ToString(),
        Title = title,
        Level = level,
        Variables = VariableHelper.Extract(title)
    };

    private static string GetArticleTitle(IElement article)
    {
        var attrTitle = article.GetAttribute("data-article-title");
        if (!string.IsNullOrWhiteSpace(attrTitle))
            return attrTitle.Trim();

        return article.QuerySelector("h1")?.TextContent.Trim() ?? string.Empty;
    }

    public static List<Clause> SegmentClauses(IDocument document, List<OutlineItem> outline, Guid templateId, DateTime now)
    {
        var flatOutline = FlattenOutline(outline);
        var clauses = new List<Clause>();
        var index = 0;

        foreach (var termBlock in document.QuerySelectorAll($"div.{TermBlockClass}"))
        {
            var text = termBlock.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var outlineItem = ResolveOutlineItem(termBlock, flatOutline, index);
            var html = termBlock.InnerHtml.Trim();
            var variables = ExtractVariables(termBlock);

            clauses.Add(new Clause
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                OutlineItemId = outlineItem?.Id,
                Text = html,
                Variables = variables.Select(v => v.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList(),
                ClauseType = termBlock.QuerySelector("h1")?.TextContent.Trim() ?? "条款",
                Keywords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(10).ToList(),
                CreatedAt = now,
                UpdatedAt = now
            });
            index++;
        }

        return clauses;
    }

    private static OutlineItem? ResolveOutlineItem(IElement termBlock, List<OutlineItem> flatOutline, int index)
    {
        var heading = termBlock.QuerySelector("h1, h2, h3, h4, h5");
        if (heading is not null)
        {
            var title = heading.TextContent.Trim();
            var match = flatOutline.FirstOrDefault(o => o.Title == title);
            if (match is not null)
                return match;
        }

        return index < flatOutline.Count ? flatOutline[index] : flatOutline.LastOrDefault();
    }

    public static QuestionContext BuildQuestionContext(IDocument document, List<OutlineItem> outline)
    {
        var articles = document.QuerySelectorAll("article")
            .Select(a => new TextSectionInfo(
                GetArticleTitle(a),
                GetDataDescription(a, "data-article-desc", "data-article-description")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .ToList();

        var termGroups = document.QuerySelectorAll($"section.{TermGroupClass}")
            .Select(s => new TextSectionInfo(
                GetDataDescription(s, "data-title", "data-group-title") is { Length: > 0 } title
                    ? title
                    : s.QuerySelector("h1,h2,h3")?.TextContent.Trim() ?? string.Empty,
                GetDataDescription(s, "data-desc", "data-description", "data-group-desc")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .ToList();

        var variables = document.QuerySelectorAll("variable")
            .Select(ParseVariable)
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .DistinctBy(v => v.Name)
            .ToList();

        return new QuestionContext(outline, articles, termGroups, variables);
    }

    public static List<VariableInfo> ExtractVariables(IElement root)
    {
        return root.QuerySelectorAll("variable")
            .Select(ParseVariable)
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .DistinctBy(v => v.Name)
            .ToList();
    }

    private static VariableInfo ParseVariable(IElement element)
    {
        var name = GetDataDescription(element, "data-name", "data-variable-name", "data-var-name");
        var description = GetDataDescription(element, "data-desc", "data-description", "data-variable-desc", "data-var-desc");
        var defaultValue = element.TextContent.Trim();
        return new VariableInfo(name, description, defaultValue);
    }

    private static string GetDataDescription(IElement element, params string[] attributeNames)
    {
        foreach (var name in attributeNames)
        {
            var value = element.GetAttribute(name);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

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

    private static string NormalizeWhitespace(string text) =>
        WhitespaceCollapse().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapse();
}
