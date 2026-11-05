---
title: Cratis Arc command causation
description: Cratis Arc records command names and values in event causation. Use PII and NotAudited annotations to keep sensitive values out of that audit record.
---

Cratis Arc records the command's name and values in the causation chain of every event it appends. That chain explains what led to the event, so you can trace a purchase order back to the command invocation that raised it.

Before adding a command property, decide whether its value belongs in that audit record. Use `[PII]` for personal data or `[NotAudited]` for other values you need to exclude.

> [!IMPORTANT]
> Causation is stored in the immutable event log. Changing a command or adding an exclusion attribute later does not remove values already recorded. Review [which values to exclude](#keeping-a-value-out-of-the-record) before the command runs with sensitive data.

## Who caused the event

When you execute a command inside `ISystemExecution.As(principal)` without an HTTP request, Chronicle records that authenticated principal as the event's `causedBy` identity. It uses the same claims as Chronicle's HTTP provider: `sub` for the subject, `name` for the name, and `preferred_username` for the username; missing name and username claims fall back to `principal.Identity.Name`.

During an HTTP request, Chronicle keeps its HTTP identity resolution; `ISystemExecution.As` does not replace the request's user.

Outside HTTP, an explicitly selected non-System Chronicle identity takes precedence. In a reactor, use `ISystemExecution.As(principal)` to replace the System identity while retaining the triggering event's `OnBehalfOf` chain. Without that override scope, an explicitly selected System identity stays System until you clear it. If there is no authenticated principal, Chronicle leaves the identity unchanged.

### When someone acts on behalf of the principal

When the principal's identity has an actor (`ClaimsIdentity.Actor` is set), the recorded identity is a chain: the actor first, then the principal as its `OnBehalfOf`. Chronicle reports such an event's initiator as an agent acting on behalf of that user. This applies inside an HTTP request too, unless code has set an identity explicitly.

This mapping applies to every authentication scheme, including [AuthProxy on-behalf sessions](../../asp-net-core/acting-on-behalf-of.md). For example, a service can exchange a user's token and present a bearer token with an RFC 8693 `act` claim. If the JWT handler maps that claim to `ClaimsIdentity.Actor`, causation records the service as `causedBy`, acting on behalf of the token's subject.

Check any authentication scheme that sets `ClaimsIdentity.Actor`: even if it uses the property for another purpose, the actor it supplies becomes the recorded cause.

## What gets recorded

For a command like this:

```csharp
[Command]
public record RaisePurchaseOrder(PurchaseOrderId OrderId, SupplierId Supplier, decimal Amount)
{
    public PurchaseOrderRaised Handle() => new(Supplier, Amount);
}
```

the causation link carries:

| Property              | Value                                  |
| --------------------- | -------------------------------------- |
| `commandType`         | `RaisePurchaseOrder`                   |
| `commandTypeFullName` | `Acme.Purchasing.RaisePurchaseOrder`   |
| `eventSequenceId`     | `event-log`                            |
| `orderId`             | `00000026-0000-0000-0000-0000000000b2` |
| `supplier`            | `ACME`                                 |
| `amount`              | `1234.56`                              |

Arc records readable public instance properties under their camel-cased names, except excluded properties and names reserved for causation metadata. Values are formatted as follows:

- A [concept](/fundamentals/csharp/concepts/) records the value it wraps, not the wrapper — `orderId` above is the id, not `{"Value":"…"}`.
- Numbers and dates are written invariantly, dates round-trippably (`2026-02-26T11:03:00.0000000+00:00`), so a chain written in one locale reads the same in another.
- A nested object or a collection is written as compact JSON. Exclusion is not recursive: `[PII]` or `[NotAudited]` on a nested member does not remove it from that JSON. Exclude the containing command property, its type, or the whole command when nested data is sensitive.
- A null value is omitted. A value that cannot be read is also left out.

## Keeping a value out of the record

Two markings keep a top-level command value off the chain. The snippets below are attribute-focused fragments using application types; commands shown without `Handle()` are not complete runnable commands.

### Personal data — `[PII]`

Arc excludes annotated top-level command values from causation. This does **not** remove them from an event you construct: event encryption separately requires PII metadata on the serialized event property/type or a shared concept.

You can mark the property, the command, the positional parameter or the property's type. Marking a shared concept applies the exclusion to every command property that uses it:

```csharp
[PII("The name of a person")]
public record ClaimantName(string Value) : ConceptAs<string>(Value);

[Command]
public record SubmitClaim(ClaimId ClaimId, ClaimantName Claimant);  // claimant is never recorded
```

See [Compliance](../compliance/pii.md) for the full picture.

### Secrets — `[NotAudited]`

Use `[NotAudited]` for values such as passwords or API keys that must not enter causation. Choose `[PII]` when the value represents personal data; the categories can overlap. Neither annotation makes storing credentials in an event safe:

```csharp
[Command]
public record ChangePassword(
    UserId User,
    [property: NotAudited] string OldPassword,
    [property: NotAudited] string NewPassword)
{
    public PasswordChanged Handle(IPasswordHasher hasher) => new(hasher.Hash(NewPassword));
}
```

Written on a positional parameter — `[NotAudited] string OldPassword` — it works the same way.

Mark a shared concept to exclude it wherever a command uses it:

```csharp
[NotAudited]
public record ProviderApiKey(string Value) : ConceptAs<string>(Value);
```

Mark the whole command when none of its property values should be recorded. The exclusion also covers properties added later:

```csharp
[Command]
[NotAudited]
public record ResetCredentials(UserId User, string Password, string RecoveryCode);
```

Excluding values does not exclude the command's name. The chain still identifies which command produced the events.

## The analyzer

[ARCCHR0009](../code-analysis/ARCCHR0009.md) reports unmarked command properties whose names and types suggest secrets. It leaves a `DateTimeOffset` called `AccessTokenExpiresAt` alone because that value is a timestamp.

If a reported value belongs in the audit record, [suppress the diagnostic](../code-analysis/ARCCHR0009.md#when-the-rule-is-wrong) rather than adding `[NotAudited]`: the attribute would withhold the value. Review the other properties yourself too. An API key stored in a property named `Value` will not be caught by a name-based check.

## Size

Arc keeps the first 1024 characters of a long value and appends `…` to mark the truncation. Causation is repeated on every event the command appends, so keep recorded values small enough to serve as audit context rather than event payloads.

## Reading the chain

In the [Chronicle Workbench](/chronicle/workbench/), an event's **Context** tab shows its causation. Open `causation`, then an entry's `properties`, to see what the command was asked to do.

## See also

- [ARCCHR0009](../code-analysis/ARCCHR0009.md) — the analyzer for unmarked secrets
- [Compliance](../compliance/pii.md) — marking personal data
- [Events](events.md) — what a command returns
