---
name: sf-purchases
description: Use when configuring SFramework products, purchase results, price metadata, restoration, or ISFPurchasesService integrations.
---

# sf-purchases

Read [the package guide](references/package-readme.md) and [installed source](SOURCE.md) before choosing a purchase implementation.

Register SFMockPurchasesService for sandbox use. The compatibility name SFPurchasesService provides the same simulation. Known IDs immediately confirm unless cancellation is requested. Price and currency are placeholders, and restore reports success without querying ownership.

Initialize Configs before the purchase service, keep Purchases arrays non-null, and pass the exact case-sensitive product FullId to PurchaseAsync. StoreId and per-store fields are separate metadata.

Handle Confirmed, Failed, Deferred and Canceled result statuses. Use Success to test confirmation because it rejects default results. Simulated success has no transaction ID or receipt. Parsing Android receipt fields only extracts their values.

For actual billing, implement the host's store adapter, validation and entitlement/grant ownership according to project requirements. Avoid granting real entitlements solely on a stub confirmation; production duplicate prevention needs a real transaction contract.

Currency symbols can be ambiguous and are cached by ISO code. Preserve the ISO code where needed.

Verify local product/result behavior separately from real store callbacks, cancellation, restore and fulfillment. Package unit tests do not establish payment processing.
