using GetCode.Domain.Catalog;

namespace GetCode.UnitTests;

/// <summary>
/// M03-001: canonical catalog invariants — GetCode-owned stable keys, owned
/// display metadata, auditable availability/order changes.
/// </summary>
public sealed class CountryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Upsert_normalizes_code_and_raises_event()
    {
        var country = Country.Upsert("ir", "Iran", Now);

        Assert.Equal("IR", country.Code);
        Assert.False(country.IsEnabled, "new entries start disabled until an admin enables them");
        Assert.Contains(country.DomainEvents, e => e is CountryUpserted);
    }

    [Fact]
    public void Upsert_accepts_explicit_stable_id_for_seeding()
    {
        var stableId = Guid.CreateVersion7();
        var country = Country.Upsert("DE", "Germany", Now, stableId);

        Assert.Equal(stableId, country.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("I")]
    [InlineData("IRR")]
    [InlineData("I1")]
    [InlineData("i r")]
    public void Invalid_country_codes_are_rejected(string code)
    {
        Assert.ThrowsAny<ArgumentException>(() => Country.Upsert(code, "Anywhere", Now));
    }

    [Fact]
    public void Availability_change_is_idempotent_and_audited()
    {
        var country = Country.Upsert("de", "Germany", Now);

        country.SetAvailability(true, Now);
        country.SetAvailability(true, Now); // second toggle: no duplicate event
        var events = country.DomainEvents.OfType<CatalogAvailabilityChanged>().ToList();

        Assert.True(country.IsEnabled);
        Assert.Single(events);
        Assert.Equal("country", events[0].Kind);
        Assert.Equal("DE", events[0].StableKey);
    }

    [Fact]
    public void Display_order_must_be_non_negative()
    {
        var country = Country.Upsert("de", "Germany", Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => country.SetDisplayOrder(-1, Now));
    }

    [Fact]
    public void Localized_names_override_default_per_culture()
    {
        var country = Country.Upsert("de", "Germany", Now);
        country.SetLocalizedNames(
        [
            LocalizedCatalogName.Create("fa", "\u0622\u0644\u0645\u0627\u0646"),
            LocalizedCatalogName.Create("en-GB", "United Kingdom of Germany"),
        ]);

        Assert.Equal("\u0622\u0644\u0645\u0627\u0646", country.DisplayNameFor("fa"));
        Assert.Equal("United Kingdom of Germany", country.DisplayNameFor("en-gb"));
        Assert.Equal("Germany", country.DisplayNameFor("fr")); // falls back to default
    }

    [Fact]
    public void Duplicate_culture_names_are_deduplicated_keeping_first()
    {
        var country = Country.Upsert("de", "Germany", Now);
        country.SetLocalizedNames(
        [
            LocalizedCatalogName.Create("fa", "first"),
            LocalizedCatalogName.Create("FA", "second"),
        ]);

        Assert.Single(country.LocalizedNames);
        Assert.Equal("first", country.DisplayNameFor("fa"));
    }
}

public sealed class ServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Upsert_normalizes_slug()
    {
        var service = Service.Upsert("Telegram", "Telegram", Now);

        Assert.Equal("telegram", service.Slug);
        Assert.Contains(service.DomainEvents, e => e is ServiceUpserted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--dash")]
    [InlineData("with space")]
    public void Invalid_slugs_are_rejected(string slug)
    {
        Assert.ThrowsAny<ArgumentException>(() => Service.Upsert(slug, "Whatever", Now));
    }

    [Fact]
    public void Mixed_case_slugs_are_normalized_not_rejected()
    {
        // Case differences are normalized away; only structure is invalid.
        Assert.Equal("whatsapp", Service.Upsert("WhatsApp", "WhatsApp", Now).Slug);
    }

    [Fact]
    public void Rename_updates_display_metadata_without_changing_key()
    {
        var service = Service.Upsert("whatsapp", "WhatsApp", Now);
        var idBefore = service.Id;

        service.Rename("WhatsApp Messenger", Now.AddMinutes(1));

        Assert.Equal(idBefore, service.Id);
        Assert.Equal("whatsapp", service.Slug);
        Assert.Equal("WhatsApp Messenger", service.DefaultDisplayName);
    }
}

public sealed class LocalizedCatalogNameTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fa-IR")]
    [InlineData("zh-Hans")]
    public void Valid_culture_codes_are_accepted(string culture)
    {
        Assert.NotNull(LocalizedCatalogName.Create(culture, "Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("eng")]
    [InlineData("e1")]
    [InlineData("fa_IR")]
    public void Invalid_culture_codes_are_rejected(string culture)
    {
        Assert.ThrowsAny<ArgumentException>(() => LocalizedCatalogName.Create(culture, "Name"));
    }

    [Fact]
    public void Overlong_display_names_are_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            LocalizedCatalogName.Create("en", new string('x', LocalizedCatalogName.MaxDisplayNameLength + 1)));
    }
}
