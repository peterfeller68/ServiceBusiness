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

    [Fact]
    public void Global_material_partition_uses_material_catalog_scope()
    {
        var partition = AzureTableServiceBusinessStore.MaterialPartition("global");

        Assert.Equal("MATERIALS_GLOBAL_global", partition);
    }

    [Fact]
    public void Company_material_partition_keeps_company_scope()
    {
        var partition = AzureTableServiceBusinessStore.MaterialPartition("clearwater");

        Assert.Equal("COMPANY_clearwater", partition);
    }

    [Fact]
    public void Global_service_partition_uses_service_catalog_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePartition("global");

        Assert.Equal("SERVICES_Global_global", partition);
    }

    [Fact]
    public void Company_service_partition_uses_service_company_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePartition("clearwater");

        Assert.Equal("SERVICES_Company_clearwater", partition);
    }
}
