# Concepts

Arc uses [concept types](../core/concepts.md) as strongly-typed wrappers around primitives such as `Guid`, `string`, or `int`. Without special handling, the generated API schema would expose these as complex objects with a single `Value` property—which is rarely what API consumers expect.

The `ConceptSchemaTransformer` detects any schema type that inherits from `ConceptAs<T>` and replaces the schema with the equivalent JSON primitive type of the underlying value.

## Example

```csharp
public record CustomerId(Guid Value) : ConceptAs<Guid>(Value);
```

Without the transformer the schema for `CustomerId` would be:

```json
{
  "type": "object",
  "properties": {
    "value": { "type": "string", "format": "uuid" }
  }
}
```

With the transformer the schema becomes:

```json
{ "type": "string", "format": "uuid" }
```

This applies wherever the concept type appears — as a request parameter, in a request body, or in a response schema.

## Supported Underlying Types

The transformer maps any primitive or common .NET type that `ConceptAs<T>` supports:

| .NET type | JSON Schema type | Format |
|-----------|-----------------|--------|
| `Guid` | `string` | `uuid` |
| `string` | `string` | — |
| `int` / `long` | `integer` | `int32` / `int64` |
| `float` / `double` | `number` | `float` / `double` |
| `bool` | `boolean` | — |
| `DateTimeOffset` / `DateTime` | `string` | `date-time` |

