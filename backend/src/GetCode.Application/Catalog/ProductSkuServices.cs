using GetCode.Domain.Catalog;

namespace GetCode.Application.Catalog;

public interface IProductSkuRepository
{
    /// <summary>Looks up by the canonical (country, service, product type) identity.</summary>
    Task<ProductSku?> FindAsync(Guid countryId, Guid serviceId, ProductType productType, CancellationToken cancellationToken);
    void Add(ProductSku sku);
    Task<IReadOnlyList<ProductSku>> ListOfferedAsync(CancellationToken cancellationToken);
}

public sealed record UpsertProductSkuCommand(string CountryCode, string ServiceSlug, ProductType ProductType, string? CorrelationId = null);

public sealed record SetProductSkuOfferedCommand(string CountryCode, string ServiceSlug, ProductType ProductType, bool Offered, string? CorrelationId = null);

/// <summary>
/// Storefront read model. Composed display names come from canonical catalog
/// entries; no provider information exists on this surface.
/// </summary>
public sealed record ProductSkuView(
    string StableKey,
    string CountryCode,
    string ServiceSlug,
    string CountryDisplayName,
    string ServiceDisplayName,
    ProductType ProductType);

/// <summary>
/// Admin use cases for product SKUs. Commands address entries by canonical
/// codes/slugs; the service resolves them to aggregate ids.
/// </summary>
public sealed class ProductSkuAdminService(
    ICountryRepository countries,
    IServiceRepository services,
    IProductSkuRepository skus,
    IOutboxCollector outbox,
    ICatalogUnitOfWork unitOfWork)
{
    public async Task<Guid> UpsertAsync(UpsertProductSkuCommand command, CancellationToken cancellationToken)
    {
        var country = await RequireCountryAsync(command.CountryCode, cancellationToken);
        var service = await RequireServiceAsync(command.ServiceSlug, cancellationToken);

        var sku = await skus.FindAsync(country.Id, service.Id, command.ProductType, cancellationToken);
        if (sku is null)
        {
            sku = ProductSku.Upsert(country.Id, service.Id, command.ProductType, DateTimeOffset.UtcNow);
            skus.Add(sku);
        }

        CollectEvents(sku, country.Code, service.Slug, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return sku.Id;
    }

    public async Task SetOfferedAsync(SetProductSkuOfferedCommand command, CancellationToken cancellationToken)
    {
        var country = await RequireCountryAsync(command.CountryCode, cancellationToken);
        var service = await RequireServiceAsync(command.ServiceSlug, cancellationToken);
        var sku = await skus.FindAsync(country.Id, service.Id, command.ProductType, cancellationToken)
            ?? throw new CatalogEntryNotFoundException("product-sku", $"{country.Code}-{service.Slug}-{command.ProductType}");

        sku.SetOffered(command.Offered, DateTimeOffset.UtcNow);
        CollectEvents(sku, country.Code, service.Slug, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Country> RequireCountryAsync(string code, CancellationToken cancellationToken) =>
        await countries.FindByCodeAsync(code, cancellationToken)
        ?? throw new CatalogEntryNotFoundException("country", code);

    private async Task<Service> RequireServiceAsync(string slug, CancellationToken cancellationToken) =>
        await services.FindBySlugAsync(slug, cancellationToken)
        ?? throw new CatalogEntryNotFoundException("service", slug);

    private void CollectEvents(ProductSku sku, string countryCode, string serviceSlug, string? correlationId)
    {
        foreach (var domainEvent in sku.DomainEvents)
        {
            var type = domainEvent switch
            {
                ProductSkuUpserted => "catalog.product_sku.upserted",
                ProductSkuAvailabilityChanged => "catalog.product_sku.availability_changed",
                _ => "catalog.product_sku.changed",
            };

            outbox.Collect(type, System.Text.Json.JsonSerializer.Serialize(new
            {
                productSkuId = sku.Id,
                countryCode,
                serviceSlug,
                productType = sku.ProductType.ToString(),
                eventType = domainEvent.GetType().Name,
            }, CatalogAdminService.PayloadOptions), correlationId);
        }

        sku.ClearDomainEvents();
    }
}

/// <summary>
/// Storefront listing of offered SKUs with names resolved from the canonical
/// catalog in the requested culture.
/// </summary>
public sealed class ProductCatalogQueryService(
    ICountryRepository countries,
    IServiceRepository services,
    IProductSkuRepository skus)
{
    public async Task<IReadOnlyList<ProductSkuView>> ListOfferedSkusAsync(string cultureCode, CancellationToken cancellationToken)
    {
        var countryList = await countries.ListAsync(false, cancellationToken);
        var serviceList = await services.ListAsync(false, cancellationToken);
        var countryById = countryList.ToDictionary(c => c.Id);
        var serviceById = serviceList.ToDictionary(s => s.Id);

        var views = new List<ProductSkuView>();
        foreach (var sku in await skus.ListOfferedAsync(cancellationToken))
        {
            if (!countryById.TryGetValue(sku.CountryId, out var country) || !serviceById.TryGetValue(sku.ServiceId, out var service))
            {
                continue; // SKU references a disabled/removed catalog entry: not offered.
            }

            views.Add(new ProductSkuView(
                sku.StableKey(country.Code, service.Slug),
                country.Code,
                service.Slug,
                country.DisplayNameFor(cultureCode),
                service.DisplayNameFor(cultureCode),
                sku.ProductType));
        }

        return views;
    }
}
