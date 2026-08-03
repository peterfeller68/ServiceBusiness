using ServiceBusiness.Infrastructure.AzureStorage;
using ServiceBusiness.Domain;

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
        var partition = AzureTableServiceBusinessStore.MaterialPartition(GlobalCatalogScope.Pool);

        Assert.Equal("MATERIALS_Pool_Global", partition);
    }

    [Fact]
    public void Landscape_global_material_partition_uses_material_catalog_scope()
    {
        var partition = AzureTableServiceBusinessStore.MaterialPartition(GlobalCatalogScope.Landscape);

        Assert.Equal("MATERIALS_LandScape_Global", partition);
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
        var partition = AzureTableServiceBusinessStore.ServicePartition(GlobalCatalogScope.Pool);

        Assert.Equal("SERVICES_Pool_Global", partition);
    }

    [Fact]
    public void Landscape_global_service_partition_uses_service_catalog_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePartition(GlobalCatalogScope.Landscape);

        Assert.Equal("SERVICES_LandScape_Global", partition);
    }

    [Fact]
    public void Company_service_partition_uses_service_company_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePartition("clearwater");

        Assert.Equal("SERVICES_Company_clearwater", partition);
    }

    [Fact]
    public void Global_service_package_partition_uses_service_catalog_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePackagePartition(GlobalCatalogScope.Pool);

        Assert.Equal("SERVICEPACKAGES_Pool_Global", partition);
    }

    [Fact]
    public void Company_service_package_partition_uses_company_scope()
    {
        var partition = AzureTableServiceBusinessStore.ServicePackagePartition("clearwater");

        Assert.Equal("SERVICEPACKAGES_Company_clearwater", partition);
    }
}
