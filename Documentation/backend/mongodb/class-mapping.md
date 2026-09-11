---
title: Class mapping
description: Customize BSON members with discovered maps and type-compatible serializers.
---

Use a class map when one document needs different BSON names or serializers from the rest of your application. Arc discovers `IBsonClassMapFor<T>` implementations during [MongoDB setup](./getting-started.md).

## Basic class map example

These are complete document/map declarations. The naming-convention opt-out prevents Arc's subsequent member-convention pass from replacing the custom `username` name.

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

Initialize once before serialization freezes maps. `AutoMap()` applies driver conventions; Arc's later member pass can overwrite manual names or serializers if a convention targets that member. Filter or ignore conflicting conventions deliberately rather than relying on registration order.

## Inheritance mapping

Mapping fragment inside `IBsonClassMapFor<Document>.Configure`, using your application's `Document`, `TextDocument`, and `ImageDocument` types:

```csharp
classMap.AutoMap();
classMap.SetIsRootClass(true);
classMap.AddKnownType(typeof(TextDocument));
classMap.AddKnownType(typeof(ImageDocument));
```

Derived maps may call `SetDiscriminator("text")` or another stable value. Discriminators are a persisted contract: test polymorphic reads and existing documents before changing them. Arc's derived-type conventions also participate where derived types are registered.

## Custom serializers in class maps

Inside a map for an entity whose `Price` is `decimal`:

```csharp
classMap.GetMemberMap(product => product.Price)
    .SetSerializer(new MongoDB.Bson.Serialization.Serializers.DecimalSerializer(BsonType.Decimal128));
```

For an enum property, use `EnumSerializer<YourEnum>(BsonType.String)` if strings are the intended persisted representation. For dictionaries and `object` members, choose a driver serializer compatible with the member's declared type and explicit allowed types; do not use nonexistent `ObjectSerializationOptions` constructor arguments or an unrestricted polymorphic allowlist by default.

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

Continue with [concepts](./concepts.md) or [convention packs](./convention-packs.md).
