using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.Integrations;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class StripePaymentProviderGatewayTests
{
    [Fact]
    public void Stripe_webhook_parser_validates_signature_and_extracts_checkout_session()
    {
        const string secret = "whsec_test";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:Stripe:WebhookSecret"] = secret,
                ["Payment:Stripe:Mode"] = "test"
            })
            .Build();
        using var httpClient = new HttpClient();
        var gateway = new StripePaymentProviderGateway(httpClient, configuration);
        var payload = """
        {
          "id": "evt_123",
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "cs_test_123",
              "client_reference_id": "owner-123",
              "customer": "cus_123",
              "subscription": "sub_123",
              "metadata": {
                "owner_user_id": "owner-123"
              }
            }
          }
        }
        """;
        var signature = CreateStripeSignature(payload, secret);

        var parsed = gateway.ParseWebhookEvent(payload, signature);

        Assert.Equal("Stripe", parsed.Provider);
        Assert.Equal("test", parsed.ProviderMode);
        Assert.Equal("evt_123", parsed.ProviderEventId);
        Assert.Equal("checkout.session.completed", parsed.EventType);
        Assert.Equal("owner-123", parsed.OwnerUserId);
        Assert.Equal("cus_123", parsed.ProviderCustomerId);
        Assert.Equal("sub_123", parsed.ProviderSubscriptionId);
        Assert.Equal("cs_test_123", parsed.ProviderCheckoutSessionId);
        Assert.Equal(SubscriptionStatus.Active, parsed.SubscriptionStatus);
    }

    [Fact]
    public void Stripe_webhook_parser_rejects_invalid_signature()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:Stripe:WebhookSecret"] = "whsec_test"
            })
            .Build();
        using var httpClient = new HttpClient();
        var gateway = new StripePaymentProviderGateway(httpClient, configuration);

        Assert.Throws<InvalidOperationException>(() =>
            gateway.ParseWebhookEvent("""{"id":"evt_123","type":"ping","data":{"object":{}}}""", "t=123,v1=bad"));
    }

    private static string CreateStripeSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        return $"t={timestamp},v1={expected}";
    }
}
