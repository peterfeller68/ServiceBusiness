using ServiceBusiness.Web;

namespace ServiceBusiness.Tests;

public sealed class UserGuideContentServiceTests
{
    [Fact]
    public void Normalize_slug_rejects_path_traversal()
    {
        Assert.Equal("index", UserGuideContentService.NormalizeSlug("../secrets"));
    }

    [Fact]
    public void GetArticle_loads_markdown_title_and_renders_content()
    {
        var guideRoot = CreateGuideRoot(("getting-started.md", "# Getting Started\n\nWelcome to Help."));
        var service = new UserGuideContentService(guideRoot);

        var article = service.GetArticle("getting-started");

        Assert.Equal("Getting Started", article.Title);
        Assert.Contains("<h1>Getting Started</h1>", article.Html.Value);
        Assert.Contains("<p>Welcome to Help.</p>", article.Html.Value);
    }

    [Fact]
    public void RenderMarkdown_routes_user_guide_links_to_help_pages()
    {
        var html = UserGuideContentService.RenderMarkdown("- [Invoices](invoices.md)");

        Assert.Contains("<a href=\"help/user-guide/invoices\">Invoices</a>", html.Value);
    }

    private static string CreateGuideRoot(params (string FileName, string Contents)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), "service-business-guide-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        foreach (var file in files)
        {
            File.WriteAllText(Path.Combine(root, file.FileName), file.Contents);
        }

        return root;
    }
}
