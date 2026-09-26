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

With ASP.NET Core's default numeric serialization for plain minimal APIs (and Arc's own default numeric serialization for its endpoints), the schema is:

```json
{
  "type": "integer",
  "enum": [0, 1, 2, 3]
}
```

If the effective HTTP JSON options use a `JsonStringEnumConverter` instead, the schema uses `"type": "string"` and `"enum": ["Draft", "Sent", "Paid", "Overdue"]`. Verify both the response and generated schema after changing converters.

## Configuration boundary

Arc-generated endpoints serialize with `ArcOptions.JsonSerializerOptions`. ASP.NET's `ConfigureHttpJsonOptions` configures plain minimal API endpoints, not Arc-generated endpoints. Plain minimal APIs retain ASP.NET Core's enum serialization by default, including long-backed values greater than `int.MaxValue`; Arc does not append its enum converter there. A `JsonStringEnumConverter` registered with `ConfigureHttpJsonOptions` writes strings for plain minimal APIs and changes their schema to string values. The first matching application converter wins. The OpenAPI transformer uses the JSON options for the schema's endpoint; keep them aligned with the endpoint's actual response.

## See also

- [JSON serialization configuration](../asp-net-core/configuration.md#json-serialization) — choose the correct options target.
- [OpenAPI setup](index.md) — ASP.NET transformer registration.
- [Lightweight OpenAPI](../core/openapi.md) — a different, metadata-only implementation.
