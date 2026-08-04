using ServiceBusiness.Web;

namespace ServiceBusiness.Tests;

public sealed class InvoiceHtmlDisplayTests
{
    [Fact]
    public void Render_preserves_invoice_html_markup()
    {
        var invoiceHtml = "<h1>Invoice 000001</h1><p>Total: $25.00</p>";

        var rendered = InvoiceHtmlDisplay.Render(invoiceHtml);

        Assert.Equal(invoiceHtml, rendered.Value);
    }

    [Fact]
    public void Render_shows_empty_state_when_invoice_html_is_missing()
    {
        var rendered = InvoiceHtmlDisplay.Render("");

        Assert.Contains("No invoice HTML available.", rendered.Value);
    }
}
