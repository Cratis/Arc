---
uid: Arc.Chronicle.Compliance.PII
---
# PII

Arc automatically decrypts `[PII]`-annotated properties on read models before they are served to clients. This builds on top of the [Read Model Interception](../../../queries/read-model-interception.md) pipeline so decryption is applied consistently across all query types — controller-based, model-bound, and observable (WebSocket and SSE).

For the full Chronicle-level guide on annotating types, identifying subjects, and honoring erasure requests, see [Chronicle compliance](xref:Chronicle.Compliance).

## How It Works

Chronicle encrypts `[PII]` properties at the event log boundary under the subject's encryption key. Arc's interception pipeline calls `Release()` on `IReadModels` before each query response, decrypting those values transparently — no changes to query methods are needed.

Given a read model with a PII-annotated property:

```csharp
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string CompanyName,
    [PII] string ContactEmail);
```

All query endpoints that return `CustomerProfile` serve decrypted values automatically.

## Behavior on Failure

`Release()` is intentionally non-breaking:

- If the read model has **no PII-annotated properties**, the original instance is returned immediately without contacting the server.
- If the encryption key **no longer exists** (e.g. after a right-to-erasure request), the original encrypted instance is returned and an error is logged. No exception is thrown.
