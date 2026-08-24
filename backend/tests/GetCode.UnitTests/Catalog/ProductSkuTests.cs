using GetCode.Domain.Catalog;

namespace GetCode.UnitTests;

/// <summary>
/// M03-002: SKU invariants. The customer product identity is the canonical
/// (country, service, product type) triple; provider routing is never part of
/// the model, and the type set is extensible for future fulfillment kinds.
/// </summary>
public sealed class ProductSkuTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CountryId = Guid.CreateVersion7();
    private static readonly Guid ServiceId = Guid.CreateVersion7();

    [Fact]
    public void Upsert_creates_unoffered_sku_with_identity_triple()
    {
        var sku = ProductSku.Upsert(CountryId, ServiceId, ProductType.Activation, Now);

        Assert.Equal(CountryId, sku.CountryId);
        Assert.Equal(ServiceId, sku.ServiceId);
        Assert.Equal(ProductType.Activation, sku.ProductType);
        Assert.False(sku.IsOffered, "SKUs start commercially unavailable");
        Assert.Contains(sku.DomainEvents, e => e is ProductSkuUpserted);
    }

    [Fact]
    public void Stable_key_is_derived_from_canonical_parts()
    {
        var sku = ProductSku.Upsert(CountryId, ServiceId, ProductType.Rental, Now);

        Assert.Equal("IR-telegram-rental", sku.StableKey("ir", "Telegram"));
    }

    [Theory]
    [InlineData(ProductType.Activation)]
    [InlineData(ProductType.Rental)]
    public void Defined_product_types_are_accepted(ProductType productType)
    {
        Assert.NotNull(ProductSku.Upsert(CountryId, ServiceId, productType, Now));
    }

    [Fact]
    public void Undefined_product_type_value_is_rejected()
    {
        var undefined = (ProductType)99;
        Assert.ThrowsAny<ArgumentException>(() => ProductSku.Upsert(CountryId, ServiceId, undefined, Now));
    }

    [Fact]
    public void Empty_catalog_references_are_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => ProductSku.Upsert(Guid.Empty, ServiceId, ProductType.Activation, Now));
        Assert.ThrowsAny<ArgumentException>(() => ProductSku.Upsert(CountryId, Guid.Empty, ProductType.Activation, Now));
    }

    [Fact]
    public void Offering_toggle_is_idempotent_and_audited()
    {
        var sku = ProductSku.Upsert(CountryId, ServiceId, ProductType.Activation, Now);

        sku.SetOffered(true, Now);
        sku.SetOffered(true, Now); // no duplicate event

        var changes = sku.DomainEvents.OfType<ProductSkuAvailabilityChanged>().ToList();
        Assert.Single(changes);
        Assert.True(changes[0].Offered);
        Assert.True(sku.IsOffered);

        sku.SetOffered(false, Now.AddMinutes(1));
        Assert.Equal(2, sku.DomainEvents.OfType<ProductSkuAvailabilityChanged>().Count());
        Assert.False(sku.IsOffered);
    }

    /// <summary>
    /// Guard for acceptance criterion 2: the aggregate surface must not expose
    /// any provider concept. If someone adds one, this test names it.
    /// </summary>
    [Fact]
    public void Sku_surface_carries_no_provider_concepts()
    {
        var forbidden = new[] { "Provider", "Vendor", "Supplier" };
        var propertyNames = typeof(ProductSku).GetProperties().Select(p => p.Name).ToList();

        foreach (var name in propertyNames)
        {
            Assert.DoesNotContain(name, forbidden, StringComparer.OrdinalIgnoreCase);
        }
    }
}
