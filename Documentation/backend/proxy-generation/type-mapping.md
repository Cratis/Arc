# Type Mapping

The proxy generator translates the .NET types on your commands, queries and read models into TypeScript types on the generated proxies. This page records that translation, so you can tell from the C# what the browser will actually receive.

## Primitive and common types

| .NET type | TypeScript type | `@field` constructor | Imported from |
|---|---|---|---|
| `bool` | `boolean` | `Boolean` | — |
| `string`, `char`, `Uri` | `string` | `String` | — |
| `byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `decimal` | `number` | `Number` | — |
| `DateTime`, `DateTimeOffset` | `Date` | `Date` | — |
| `DateOnly`, `TimeOnly` | `string` | `String` | — |
| `Guid` | `Guid` | `Guid` | `@cratis/fundamentals` |
| `TimeSpan` | `TimeSpan` | `TimeSpan` | `@cratis/fundamentals` |
| `Cratis.Geospatial.Point`, `LineString`, `Polygon` | same name | same name | `@cratis/fundamentals` |
| `object`, `JsonNode`, `JsonObject`, `JsonArray`, `JsonDocument` | `Record<string, unknown>` | `Object` | — |

An enum becomes a TypeScript `enum` and travels as its underlying number. A `ConceptAs<T>` is unwrapped to `T` and mapped by this same table. A `Nullable<T>` is unwrapped to `T` and the generated property is declared optional.

Collections become arrays. A dictionary becomes `Record<string, TValue>` when its key maps to `string`, and `ValueMap<TKey, TValue>` otherwise.

## Dates, times and instants

`DateTime` and `DateTimeOffset` denote instants, so they map to the JavaScript `Date` that also denotes one.

`DateOnly` and `TimeOnly` do not. A calendar date has no time and no zone; a time of day has no date. Both cross the wire as their ISO-8601 string — `"2026-05-12"` and `"14:30:45"` — and the generated proxy hands that string through unchanged.

This is deliberate, because a `Date` cannot hold either value without inventing an instant that was never sent:

```typescript
new Date('2026-05-12')      // 2026-05-12T00:00:00.000Z — UTC midnight, an instant nobody sent
new Date('14:30:45')        // Invalid Date — a time of day is not a date at all
```

The first is the more dangerous of the two, because it looks like it worked. UTC midnight read back through any browser-local getter reports the *previous* day everywhere west of UTC, while remaining correct at or east of it:

```typescript
const dueDate = new Date('2026-05-12');
dueDate.toLocaleDateString('en-CA', { timeZone: 'Europe/Oslo' });      // '2026-05-12'
dueDate.toLocaleDateString('en-CA', { timeZone: 'America/New_York' }); // '2026-05-11'  ← wrong
```

Keeping the value a string means no zone conversion can happen behind your back. Where you do need a `Date` — to feed a date picker, or to do calendar arithmetic — construct one explicitly, and the timezone decision stays visible at the call site that makes it:

```typescript
// Parse the calendar date in the zone you actually mean, rather than in whichever
// one the deserializer would have picked for you.
const [year, month, day] = readModel.dueDate.split('-').map(Number);
const localMidnight = new Date(year, month - 1, day);
```

> [!NOTE]
> Because both map to `string`, a `DateOnly` property is not distinguishable from an ordinary `string` property by its TypeScript type alone. Distinguishing them would need a dedicated wire type in `@cratis/fundamentals`, which today ships no `DateOnly` or `TimeOnly` type and no way for a consumer to register a converter for one.

## Unmapped types

A type not in the table above is generated as its own TypeScript class, in a file mirroring its namespace, and imported into whatever references it. Types from assemblies configured as package-mapped are imported from that package instead of being generated.
