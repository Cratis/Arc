---
title: "ARCCHR0009: Command property reads as a secret and should be marked [NotAudited]"
description: A command carries a property whose name reads like a secret, and its value will be written to the causation of every event the command appends.
---

## Rule

A command's property values are recorded on the [causation chain](../commands/causation.md) of every event it appends. This rule fires when a `[Command]` type has a public property whose name contains a word that reads as a secret — `Password`, `Token`, `ApiKey`, `Credential`, `Pin`, `Cvv` and the like — and neither the property, its positional parameter, nor the command is marked `[NotAudited]` or `[PII]`.

It matches whole words, not fragments: `PasswordPolicyId` is reported, `Passenger` and `Subtotal` are not. It also judges the type, because a name is weak evidence on its own — `AccessTokenExpiresAt` holds a `DateTimeOffset` and is a timestamp, not a secret, so a property whose type is a date, a duration, a number, a bool, a `Guid` or an enum is never reported however it is named. A concept is judged by the value it wraps, since that is what gets recorded.

It stays silent on anything that is not a command, and in a project without Chronicle, where nothing is written to a causation chain at all.

## Severity

Warning

## Example

### Violation

```csharp
using Cratis.Arc.Commands.ModelBound;

[Command]
public record ChangePassword(Guid UserId, string Password)
{
    // ARCCHR0009: 'Password' is written to the causation of every event this command appends
    public PasswordChanged Handle(IPasswordHasher hasher) => new(hasher.Hash(Password));
}
```

### Fix

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record ChangePassword(Guid UserId, [property: NotAudited] string Password)
{
    public PasswordChanged Handle(IPasswordHasher hasher) => new(hasher.Hash(Password));
}
```

Marking the positional parameter — `[NotAudited] string Password` — works the same way. A command that exists only to carry secrets is marked once, on the type:

```csharp
[Command]
[NotAudited]
public record ResetCredentials(Guid UserId, string Password, string RecoveryCode);
```

If the value is personal data rather than a secret, mark it `[PII]` instead — Chronicle withholds that from the causation too, and encrypts it in the event.

When the secret has its own concept, mark the concept once and every command that takes one is covered:

```csharp
[NotAudited]
public record ProviderApiKey(string Value) : ConceptAs<string>(Value);
```

## Quick Fix

None. The right response depends on what the value is: `[NotAudited]` for a secret, `[PII]` for personal data, [a suppression](#when-the-rule-is-wrong) for a false positive. The two attributes are not interchangeable — `[PII]` also encrypts the value and enrolls it in erasure, which is wrong for a password, and `[NotAudited]` does nothing for a GDPR request, which is wrong for a name.

## Why This Rule Exists

A name-based guess is normally a poor basis for a diagnostic. It earns its place here because of what is at the other end of it: the causation is written into the event log, the event log is immutable, and a secret recorded there cannot be taken back out by changing code. Fixing it after the fact means redacting events. The cost of a false positive is one attribute; the cost of a miss is permanent.

The mistake is also easy to make silently. Adding a property to a command is an ordinary edit, nothing about it says "this is now in the audit trail forever", and the value only appears somewhere a person would notice — the Workbench, a replay, an export — long after the commit that introduced it.

## When the Rule Is Wrong

A property can read as a secret and hold something you do want recorded. **Suppress the diagnostic — do not mark it `[NotAudited]`:**

```csharp
[SuppressMessage("Arc.Chronicle", "ARCCHR0009", Justification = "The token's scope names, not the token")]
public string TokenScopes { get; init; }
```

`[NotAudited]` would silence the warning by withholding the value, which is a behavior change dressed up as a suppression — the audit trail quietly loses a value you wanted in it. A suppression says "recorded on purpose" and keeps it.

## What It Does Not Catch

The rule reads names, so it only sees secrets whose names say so. A property called `Value` or `Payload` holding an API key is invisible to it. Treat a clean build as "nothing obvious was missed", not as "no secrets are recorded" — the decision about what belongs in a permanent audit record is still yours to make when you add the property.

## Related Rules

- [ARCCHR0008](ARCCHR0008.md) — Command key marked with the data annotations Key attribute

## See also

- [Causation](../commands/causation.md) — what a command records, and how to keep a value out of it
- [Compliance](../compliance/pii.md) — marking personal data
