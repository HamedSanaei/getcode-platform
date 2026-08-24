using GetCode.Domain.Common;

namespace GetCode.Domain.Catalog;

/// <summary>
/// What the customer receives. Values are stable contract data; new fulfillment
/// kinds append new members without changing existing ones.
/// </summary>
public enum ProductType
{
    /// <summary>One-shot activation for an expected service (receive SMS, complete signup).</summary>
    Activation = 0,

    /// <summary>Time-boxed rental of a dedicated number.</summary>
    Rental = 1,
}

/// <summary>
/// A sellable virtual-number offering expressed purely through canonical
/// catalog concepts (country + service + product type) and commercial
/// availability. Provider selection is routing metadata resolved at purchase
/// time (M03-005/M04) and is deliberately absent from this identity.
/// </summary>
public sealed partial class ProductSku : AggregateRoot<Guid>
{
    private ProductSku(Guid id, Guid countryId, Guid serviceId, ProductType productType)
        : base(id)
    {
        CountryId = countryId;
        ServiceId = serviceId;
        ProductType = productType;
    }

    /// <summary>EF materialization constructor.</summary>
    private ProductSku()
        : base(Guid.Empty)
    {
        CountryId = Guid.Empty;
        ServiceId = Guid.Empty;
    }

    public Guid CountryId { get; private set; }
    public Guid ServiceId { get; private set; }
    public ProductType ProductType { get; private set; }
    public bool IsOffered { get; private set; }

    /// <summary>
    /// Human-readable stable key derived from canonical parts (e.g. 'IR-telegram-activation').
    /// Identity remains the (country, service, product type) triple, not this string.
    /// </summary>
    public string StableKey(string countryCode, string serviceSlug) =>
        $"{countryCode.ToUpperInvariant()}-{serviceSlug.ToLowerInvariant()}-{ProductType.ToString().ToLowerInvariant()}";

    public static ProductSku Upsert(Guid countryId, Guid serviceId, ProductType productType, DateTimeOffset nowUtc, Guid? id = null)
    {
        if (countryId == Guid.Empty)
        {
            throw new ArgumentException("A canonical country is required.", nameof(countryId));
        }

        if (serviceId == Guid.Empty)
        {
            throw new ArgumentException("A canonical service is required.", nameof(serviceId));
        }

        if (!Enum.IsDefined(productType))
        {
            throw new ArgumentException("Product type must be a defined value.", nameof(productType));
        }

        var sku = new ProductSku(id ?? Guid.CreateVersion7(), countryId, serviceId, productType);
        sku.Raise(new ProductSkuUpserted(sku.Id, nowUtc));
        return sku;
    }

    /// <summary>Commercial availability toggle; idempotent re-toggles raise nothing.</summary>
    public void SetOffered(bool offered, DateTimeOffset nowUtc)
    {
        if (IsOffered == offered)
        {
            return;
        }

        IsOffered = offered;
        Raise(new ProductSkuAvailabilityChanged(Id, offered, nowUtc));
    }
}
