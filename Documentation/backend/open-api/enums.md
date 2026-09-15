---
title: Enums
description: Understand the current mismatch between Arc's numeric enum serialization and its OpenAPI enum-name transformer.
---

Arc serializes enums numerically by default. `EnumSchemaTransformer` makes names visible in ASP.NET OpenAPI schemas, but its current implementation does not fully align the schema with that wire format.

## Example

This complete enum type fragment illustrates the contract:

```csharp
public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue
}
```

An ordinary numeric schema starts as:

```json
{
  "type": "integer",
  "enum": [0, 1, 2, 3]
}
```

The transformer clears `enum` and inserts the names. It **does not change `type`**. Given the schema above, its output is therefore:

```json
{
  "type": "integer",
  "enum": ["Draft", "Sent", "Paid", "Overdue"]
}
```

This is a current schema inconsistency, not a valid string-enum contract: none of those string members satisfies the integer type. The transformer itself does not change runtime serialization, so a normal Arc payload still carries `0` for `Draft`.

## Configuration boundary

Arc-generated endpoints serialize with `ArcOptions.JsonSerializerOptions`. ASP.NET's `ConfigureHttpJsonOptions` configures a different options instance and does not switch Arc endpoints to string enums. Appending `JsonStringEnumConverter` to Arc options also cannot be assumed to override the existing enum converter: the first matching converter wins.

Do not change your public numeric wire format just to match this schema example. If you customize schema or serialization, explicitly align both contracts and test the actual request/response plus generated schema before generating clients.

## See also

- [JSON serialization configuration](../asp-net-core/configuration.md#json-serialization) — the correct options target.
- [OpenAPI setup](index.md) — ASP.NET transformer registration.
- [Lightweight OpenAPI](../core/openapi.md) — a different, metadata-only implementation.
