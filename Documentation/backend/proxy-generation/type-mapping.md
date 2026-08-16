# Type Mapping

The proxy generator translates the .NET types on your commands, queries and read models into TypeScript types on the generated proxies. This page records that translation, so you can tell from the C# what the browser will actually receive.

## Primitive and common types

| .NET type | TypeScript type | Metadata constructor | Imported from |
|---|---|---|---|
| `bool` | `boolean` | `Boolean` | — |
| `string`, `char`, `Uri` | `string` | `String` | — |
| `byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `decimal` | `number` | `Number` | — |
| `DateTime`, `DateTimeOffset` | `Date` | `Date` | — |
| `DateOnly` | `DateOnly` | `DateOnly` | `@cratis/fundamentals` |
| `TimeOnly` | `TimeOnly` | `TimeOnly` | `@cratis/fundamentals` |
| `Guid` | `Guid` | `Guid` | `@cratis/fundamentals` |
| `TimeSpan` | `TimeSpan` | `TimeSpan` | `@cratis/fundamentals` |
| `Cratis.Geospatial.Point`, `LineString`, `Polygon` | same name | same name | `@cratis/fundamentals` |
| `object`, `JsonNode`, `JsonObject`, `JsonArray`, `JsonDocument` | `Record<string, unknown>` | `Object` | — |

The metadata constructor is passed to the generated `@field(...)` decorator, which records the runtime type used during deserialization.

An enum becomes a TypeScript `enum` and travels as its underlying number. A `ConceptAs<T>` is unwrapped to `T` and mapped by this same table. A `Nullable<T>` is unwrapped to `T` and the generated property is declared optional.

Collections become arrays. A dictionary becomes `Record<string, TValue>` when its key maps to `string`, and `ValueMap<TKey, TValue>` otherwise.

## Dates, times and instants

`DateTime` and `DateTimeOffset` denote instants, so they map to the JavaScript `Date` that also denotes one.

`DateOnly` and `TimeOnly` do not. A calendar date has no time and no zone; a time of day has no date. Both cross the wire as their ISO-8601 string — `"2026-05-12"` and `"14:30:45"` — and each has a type of its own in `@cratis/fundamentals` that holds exactly that, with no instant invented for it.

This matters because a `Date` cannot hold either value without inventing one that was never sent:

```typescript
new Date('2026-05-12')      // 2026-05-12T00:00:00.000Z — UTC midnight, an instant nobody sent
new Date('14:30:45')        // Invalid Date — a time of day is not a date at all
```

The first is the more dangerous of the two, because it looks like it worked. UTC midnight read back through any browser-local getter reports the *previous* day everywhere west of UTC, while remaining correct at or east of it — so the bug is invisible to a developer in Europe and constant for a user in the Americas:

```typescript
const asAnInstant = new Date('2026-05-12');
asAnInstant.toLocaleDateString('en-CA', { timeZone: 'Europe/Oslo' });      // '2026-05-12'
asAnInstant.toLocaleDateString('en-CA', { timeZone: 'America/New_York' }); // '2026-05-11'  ← wrong
```

`DateOnly` holds the three parts the server sent, so there is no instant to convert and nothing to shift:

```typescript
readModel.dueDate.toString();   // '2026-05-12', in every time zone
readModel.dueDate.year;         // 2026
readModel.dueDate.day;          // 12
```

Where you genuinely need a `Date` — to feed a date picker, or to do calendar arithmetic — `toDate()` constructs one at midnight in the local zone. It is a method rather than what the value is, precisely because calling it invents a time, and that choice belongs at the call site making it:

```typescript
const localMidnight = readModel.dueDate.toDate();
```

`TimeOnly` works the same way, with `hour`, `minute`, `second` and `millisecond`.

## Declaring how your own types cross the wire

The table above is the default. A `TypeToTsType` item overrides it, and is also how you declare a type the generator has never seen:

```xml
<ItemGroup>
    <TypeToTsType Include="calendar-date"
                  TypeName="System.DateOnly"
                  TsType="LocalDate"
                  Package="@acme/time" />
</ItemGroup>
```

Every `DateOnly` then generates as `LocalDate`, imported from `@acme/time`. Omit `Package` to generate a bare TypeScript type with no import.

Mappings are consulted **ahead of** the built-in table, so this corrects an existing type as readily as it declares a new one. A build that configures none generates exactly what it generated before.

:::note
The defaults are chosen to be right without configuration — reach for a mapping when your application wants a *different* type, not to work around a default that is wrong.

Whatever you map to has to be able to deserialize from what the server actually sends. `DateOnly` arrives as `"2026-05-12"` and `TimeOnly` as `"14:30:45"`, so register a converter for the type you map to with `JsonSerializer.registerConverter`, or the value arrives as the raw string wearing the declared type's name.
:::

## Unmapped types

A type not in the table above, and not declared through `TypeToTsType`, is generated as its own TypeScript class, in a file mirroring its namespace, and imported into whatever references it. Types from assemblies configured as package-mapped are imported from that package instead of being generated.
