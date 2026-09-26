---
title: Enums
description: Match OpenAPI enum schemas to the effective JSON converter's wire values.
---

Arc serializes enums numerically by default. `EnumSchemaTransformer` describes the values written by the effective JSON converter, so the OpenAPI schema agrees with the wire format.

## Example

```csharp
public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue
}
```

With Arc's default numeric serialization, the schema is:

```json
{
  "type": "integer",
  "enum": [0, 1, 2, 3]
}
```

If the effective HTTP JSON options use a `JsonStringEnumConverter` instead, the schema uses `"type": "string"` and `"enum": ["Draft", "Sent", "Paid", "Overdue"]`. Verify both the response and generated schema after changing converters.

## Configuration boundary

Arc-generated endpoints serialize with `ArcOptions.JsonSerializerOptions`. ASP.NET's `ConfigureHttpJsonOptions` configures plain minimal API endpoints, not Arc-generated endpoints. It preserves application converter precedence: a string enum converter registered before Arc's converters writes strings for plain minimal APIs. Appending a string converter after an existing matching converter cannot override that converter, because the first match wins. The OpenAPI transformer uses the JSON options for the schema's endpoint; keep them aligned with the endpoint's actual response.

## See also

- [JSON serialization configuration](../asp-net-core/configuration.md#json-serialization) — choose the correct options target.
- [OpenAPI setup](index.md) — ASP.NET transformer registration.
- [Lightweight OpenAPI](../core/openapi.md) — a different, metadata-only implementation.
