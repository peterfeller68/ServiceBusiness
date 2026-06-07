using Microsoft.Extensions.Configuration;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using ServiceBusiness.Infrastructure.Integrations;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class EmailNotificationTests
{
    [Fact]
    public async Task Approval_email_for_test_user_is_rerouted_and_logged()
    {
        var store = new InMemoryServiceBusinessStore();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:TestRecipientEmail"] = "test-inbox@example.com"
            })
            .Build();
        var queue = new AzureCommunicationEmailNotificationQueue(store, configuration);
        var user = (await store.GetUserAsync("new-tech"))!;
        var company = (await store.GetCompanyAsync("clearwater"))!;
        var role = (await store.GetRoleDefinitionsAsync()).First(r => r.Role == CompanyRole.CompanyUser);
        var membership = (await store.GetMembershipsForUserAsync(user.Id)).Single();

        await queue.QueueAccountApprovalDecisionEmailAsync(
            new AccessRequest(membership, user, company, role),
            MembershipStatus.Active);

        var email = Assert.Single(await store.GetEmailLogsAsync());
        Assert.Equal(EmailDeliveryStatus.TestRerouted, email.Status);
        Assert.Equal("pending.tech.test@example.com", email.OriginalRecipientEmail);
        Assert.Equal("test-inbox@example.com", email.RecipientEmail);
    }
}
