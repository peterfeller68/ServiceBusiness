using Microsoft.AspNetCore.Components;

namespace ServiceBusiness.Web;

public static class InvoiceHtmlDisplay
{
    public static MarkupString Render(string? invoiceHtml) =>
        new(string.IsNullOrWhiteSpace(invoiceHtml) ? "<p>No invoice HTML available.</p>" : invoiceHtml);
}
