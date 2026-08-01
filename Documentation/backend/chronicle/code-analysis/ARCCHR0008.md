---
title: "ARCCHR0008: Command key marked with the data annotations Key attribute"
description: A command in a Chronicle application marks its key with System.ComponentModel.DataAnnotations.KeyAttribute, which Chronicle does not resolve keys from.
---

## Rule

Two attributes are spelled `[Key]`, and which one a command needs depends on whether the application has Chronicle:

| Attribute | Resolved by |
|---|---|
| `Cratis.Chronicle.Keys.KeyAttribute` | Chronicle, as the command's event source id |
| `System.ComponentModel.DataAnnotations.KeyAttribute` | Arc — but only in an application with **no** Chronicle |

This rule fires when a `[Command]` type marks a property with the data annotations attribute in a project that references Chronicle. It stays silent when the property also carries Chronicle's attribute, on anything that is not a command, and in a project without Chronicle — where the data annotations attribute is the right one.

## Severity

Warning

## Example

### Violation

```csharp
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record RenameCustomer([property: Key] Guid CustomerId, string NewName)
{
    // ARCCHR0008: Chronicle does not resolve keys from this attribute
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

### Fix

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Keys;

[Command]
public record RenameCustomer([property: Key] Guid CustomerId, string NewName)
{
    public CustomerRenamed Handle(Customer customer) => new(customer.Id, NewName);
}
```

A command whose key is not one property implements `ICanProvideEventSourceId` instead.

## Quick Fix

**Use the Chronicle Key attribute** rewrites the attribute for you.

It writes the attribute out in full rather than adding a using: the file already has one for the data annotations namespace, and with both in scope a bare `[Key]` is ambiguous (`CS0104`). Remove the data annotations using yourself if nothing else in the file needs it, and the name shortens to `[Key]`.

## Why This Rule Exists

The mistake is invisible without it. The code compiles, and it reads exactly like a command that declares its key — but Chronicle finds no key property, invents a fresh event source id for the command, and every read model keyed by it resolves to nothing. What reaches the client is [`ReadModelDoesNotExistForCommand`](../read-models/failures.md#readmodeldoesnotexistforcommand) — "The command targets an entity that does not exist" — which points at the data rather than at the attribute, on a request whose entity exists perfectly well.

The two attributes are also easy to reach for by accident. The data annotations one is what an Entity Framework Core read model marks its primary key with, so it is already imported in projects that use both, and an editor completing `[Key]` offers whichever namespace is in scope.

## Related Rules

- [ARCCHR0005](ARCCHR0005.md) — Chronicle is used but not wired up

## See also

- [Read models from other providers](../read-models/other-providers.md#declaring-the-key-without-chronicle) — declaring a command's key with and without Chronicle
