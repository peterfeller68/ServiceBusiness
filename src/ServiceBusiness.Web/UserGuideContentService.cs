using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;

namespace ServiceBusiness.Web;

public sealed partial class UserGuideContentService
{
    private const string GuideDirectory = "docs/user-guide";
    private readonly string guideRoot;

    public UserGuideContentService(IWebHostEnvironment environment)
        : this(ResolveGuideRoot(environment.ContentRootPath))
    {
    }

    public UserGuideContentService(string guideRoot)
    {
        this.guideRoot = guideRoot;
    }

    public UserGuideArticle GetArticle(string slug)
    {
        var normalizedSlug = NormalizeSlug(slug);
        var path = Path.Combine(guideRoot, $"{normalizedSlug}.md");
        if (!File.Exists(path))
        {
            return new UserGuideArticle(
                "Guide Not Found",
                normalizedSlug,
                new MarkupString("<p>The requested guide is not available.</p>"));
        }

        var markdown = File.ReadAllText(path);
        return new UserGuideArticle(GetTitle(markdown, normalizedSlug), normalizedSlug, RenderMarkdown(markdown));
    }

    public static string NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "index";
        }

        var trimmed = slug.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal) ||
            trimmed.Contains('/', StringComparison.Ordinal) ||
            trimmed.Contains('\\', StringComparison.Ordinal))
        {
            return "index";
        }

        var normalized = Path.GetFileNameWithoutExtension(trimmed).ToLowerInvariant();
        return SlugPattern().IsMatch(normalized) ? normalized : "index";
    }

    public static MarkupString RenderMarkdown(string markdown)
    {
        var html = new StringBuilder();
        var inList = false;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                if (inList)
                {
                    html.AppendLine("</ul>");
                    inList = false;
                }

                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (!inList)
                {
                    html.AppendLine("<ul>");
                    inList = true;
                }

                html.Append("<li>");
                html.Append(RenderInline(line[2..].Trim()));
                html.AppendLine("</li>");
                continue;
            }

            if (inList)
            {
                html.AppendLine("</ul>");
                inList = false;
            }

            var headingLevel = GetHeadingLevel(line);
            if (headingLevel > 0)
            {
                var headingText = line[headingLevel..].Trim();
                html.Append("<h");
                html.Append(headingLevel);
                html.Append('>');
                html.Append(RenderInline(headingText));
                html.Append("</h");
                html.Append(headingLevel);
                html.AppendLine(">");
                continue;
            }

            html.Append("<p>");
            html.Append(RenderInline(line.Trim()));
            html.AppendLine("</p>");
        }

        if (inList)
        {
            html.AppendLine("</ul>");
        }

        return new MarkupString(html.ToString());
    }

    private static string ResolveGuideRoot(string contentRootPath)
    {
        var outputGuideRoot = Path.Combine(contentRootPath, GuideDirectory);
        if (Directory.Exists(outputGuideRoot))
        {
            return outputGuideRoot;
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", GuideDirectory));
    }

    private static string GetTitle(string markdown, string slug)
    {
        var titleLine = markdown
            .Replace("\r\n", "\n")
            .Split('\n')
            .FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));

        return string.IsNullOrWhiteSpace(titleLine)
            ? ToTitle(slug)
            : titleLine[2..].Trim();
    }

    private static int GetHeadingLevel(string line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        return level is >= 1 and <= 6 && line.Length > level && line[level] == ' ' ? level : 0;
    }

    private static string RenderInline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        return MarkdownLinkPattern().Replace(encoded, match =>
        {
            var text = match.Groups["text"].Value;
            var target = WebUtility.HtmlDecode(match.Groups["target"].Value);
            if (!target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            var slug = NormalizeSlug(target);
            var href = slug.Equals("getting-started", StringComparison.OrdinalIgnoreCase)
                ? "help/getting-started"
                : slug.Equals("index", StringComparison.OrdinalIgnoreCase)
                    ? "help/user-guide"
                    : $"help/user-guide/{slug}";

            return $"<a href=\"{href}\">{text}</a>";
        });
    }

    private static string ToTitle(string slug) =>
        string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\((?<target>[^)]+)\)")]
    private static partial Regex MarkdownLinkPattern();
}

public sealed record UserGuideArticle(string Title, string Slug, MarkupString Html);
