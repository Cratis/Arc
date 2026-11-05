---
title: Customize MongoDB class mapping in Cratis Arc
description: Map individual MongoDB document types in Cratis Arc to choose BSON field names, omit members, and attach serializers without changing every type.
---

Use a MongoDB class map when one document type needs different stored field names or serializers from the rest of your application. A map controls how that type becomes BSON, MongoDB's binary document format. Arc discovers your `IBsonClassMapFor<T>` implementation during [MongoDB setup](./getting-started.md).

## Basic class map example

The following document and map store `UserName` as `username`, leave out `InternalNote`, and choose a serializer for `CreatedAt`. The `IgnoreConventions` attribute keeps Arc's naming convention from replacing the custom field name after the map is configured.

```csharp
using Cratis.Arc.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public class User
{
    public ObjectId Id { get; set; }
    public required string UserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? InternalNote { get; set; }
}

public class UserClassMap : IBsonClassMapFor<User>
{
    public void Configure(BsonClassMap<User> classMap)
    {
        classMap.AutoMap();
        classMap.SetIdMember(classMap.GetMemberMap(user => user.Id));
        classMap.GetMemberMap(user => user.UserName).SetElementName("username");
        classMap.UnmapMember(user => user.InternalNote);
        classMap.GetMemberMap(user => user.CreatedAt)
            .SetSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());
    }
}
```

The serializer matches `DateTimeOffset`, not `DateTime`. Its default BSON DateTime stores UTC milliseconds, losing the original offset and submillisecond precision. A `DateTime` property instead needs a `DateTimeSerializer`; see [serializers](./serializers.md).

Unmapping a property excludes it from this document's persistence; it is not an authorization or HTTP-response redaction mechanism. Use dedicated response models for sensitive data.

## Discovery and registration

1. Arc discovers `IBsonClassMapFor<T>` types.
2. It instantiates each provider with `Activator.CreateInstance`; providers need a usable parameterless constructor, not injected services.
3. If the model already has a registered class map, Arc skips it.
4. Otherwise it registers the map using `Configure`, then applies registered **member-map conventions** again through `ApplyConventions()`.

Register maps during startup, before serialization freezes them. `AutoMap()` applies driver conventions, and Arc reapplies member conventions after your `Configure` method. If a convention targets a name or serializer you set manually, it can overwrite that choice. [Filter or ignore the conflicting convention](./convention-packs.md#filtering-conventions), as the example does for naming.

## Inheritance mapping

Mapping fragment inside `IBsonClassMapFor<Document>.Configure`, using your application's `Document`, `TextDocument`, and `ImageDocument` types:

```csharp
classMap.AutoMap();
classMap.SetIsRootClass(true);
classMap.AddKnownType(typeof(TextDocument));
classMap.AddKnownType(typeof(ImageDocument));
```

Derived maps may call `SetDiscriminator("text")` or another stable value. A discriminator is the stored value that identifies which derived type to create when reading a document. Changing it affects existing documents, so test reads of both base and derived types before making that change. Arc's derived-type conventions also participate where derived types are registered.

## Custom serializers in class maps

Inside a map for an entity whose `Price` is `decimal`:

```csharp
classMap.GetMemberMap(product => product.Price)
    .SetSerializer(new MongoDB.Bson.Serialization.Serializers.DecimalSerializer(BsonType.Decimal128));
```

For an enum property, use `EnumSerializer<YourEnum>(BsonType.String)` if you want to store enum names as strings. For dictionaries and `object` members, choose a driver serializer compatible with the declared member type. When configuring which runtime types an `object` member can hold, allow only the types your document needs.

A class map does **not** create database indexes. Use `collection.Indexes.CreateOneAsync(...)` separately during database setup.

## Convention integration

Arc exposes `classMap.ApplyConventions()` for manually reapplying registered member conventions. Normal Arc setup already calls it after `Configure`; do not repeat it without a reason. See [convention filtering](./convention-packs.md#filtering-conventions) and [naming policies](./naming-policies.md).

## Testing class maps

After normal registration, this assertion fragment verifies the example's BSON contract without connecting to MongoDB:

```csharp
var user = new User
{
    UserName = "testuser",
    CreatedAt = DateTimeOffset.UtcNow,
    InternalNote = "not persisted"
};
var document = user.ToBsonDocument();
if (!document.Contains("_id") || !document.Contains("username") || document.Contains("InternalNote"))
{
    throw new InvalidOperationException("Unexpected User BSON mapping.");
}
```

Run serialization checks in an isolated test process or initialize global BSON conventions once. Add round-trip assertions for custom serializers and legacy documents, not just field-name checks.

If several document types need the same mapping rule, move that rule into a [convention pack](./convention-packs.md). For typed IDs and other values that should be stored without wrapper documents, see [concept serialization](./concepts.md).
