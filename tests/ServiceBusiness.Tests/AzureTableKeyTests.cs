using ServiceBusiness.Infrastructure.AzureStorage;

namespace ServiceBusiness.Tests;

public sealed class AzureTableKeyTests
{
    [Fact]
    public void Storage_key_encoding_escapes_characters_rejected_by_azure_tables()
    {
        var key = AzureTableKey.ToStorageKey("USER#abc/def\\ghi?jkl!");

        Assert.Equal("USER!0023abc!002Fdef!005Cghi!003Fjkl!0021", key);
        Assert.DoesNotContain('#', key);
        Assert.DoesNotContain('/', key);
        Assert.DoesNotContain('\\', key);
        Assert.DoesNotContain('?', key);
    }

    [Fact]
    public void Storage_key_encoding_keeps_common_keys_readable()
    {
        var key = AzureTableKey.ToStorageKey("COMPANY_demo-owner-1");

        Assert.Equal("COMPANY_demo-owner-1", key);
    }
}
