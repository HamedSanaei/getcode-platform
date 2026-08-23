# Data model principles

This is a conceptual model, not a frozen database schema.

## Durable identities

Use stable opaque IDs (typically UUID/ULID strategy decided in M00) for internal records. Never use provider IDs, emails, phone numbers or mutable natural keys as primary identity.

## Important conceptual records

```text
User / Identity
SiteHost configuration (usually configuration, not tenant data)
Country
Service
Product / SKU
Provider
ProviderMapping
ProviderOfferSnapshot
Quote
Order / OrderItem
Fulfillment
Activation
Message metadata
Wallet
LedgerEntry
Payment / PaymentAttempt
Refund
OutboxMessage
IdempotencyRecord
AuditEvent
ManualReviewCase
```

## Snapshots

Orders store the commercial snapshot required to explain a historical transaction (price/currency/product identity/rules references). Historical orders must not silently change when catalog/provider price changes.

## Money

Store monetary amount using a decimal/numeric representation and an explicit currency. Currency conversion and rounding policy must be explicit and tested.

## Sensitive activation data

Phone number/message content retention is a product/privacy decision. The default architecture minimizes storage and logs masked identifiers. Raw OTP/SMS content must not enter general logs.
