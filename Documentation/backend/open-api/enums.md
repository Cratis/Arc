# Enums

By default, `System.Text.Json` serializes enums as integers. This makes API documentation less readable because consumers see numeric values rather than meaningful names.

The `EnumSchemaTransformer` replaces integer enum values in the schema with their string names, making the documentation self-explanatory without requiring additional annotations.

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

Without the transformer the schema for `InvoiceStatus` would be:

```json
{
  "type": "integer",
  "enum": [0, 1, 2, 3]
}
```

With the transformer the schema becomes:

```json
{
  "type": "string",
  "enum": ["Draft", "Sent", "Paid", "Overdue"]
}
```

## Behaviour

The transformer applies to every enum type that appears in the API schema — request bodies, response bodies, and query/route parameters alike. No additional attributes or configuration are required.

> [!NOTE]
> The transformer only modifies the OpenAPI schema. To ensure your runtime serialisation matches (i.e. that JSON actually serialises enum values as strings), add `JsonStringEnumConverter` to your serialisation options:
>
> ```csharp
> builder.Services.ConfigureHttpJsonOptions(options =>
>     options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
> ```

