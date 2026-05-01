---
uid: Arc.Chronicle.Compliance
---
# Compliance

Arc integrates with Chronicle's compliance infrastructure to enable automatic decryption of PII (Personal Identifiable Information) properties on read models before they are served to clients. This builds on top of the [Read Model Interception](../../queries/read-model-interception.md) pipeline so decryption is applied consistently across all query types — controller-based, model-bound, and observable (WebSocket and SSE).

## How Chronicle Compliance Works

Chronicle encrypts properties or properties of `ConceptAs<>` type annotated with `[PII]` at the event log boundary. When events are projected into read models the encrypted values are stored as-is. Before presenting a read model to a user you must call `Release()` on `IReadModels` to have the Chronicle kernel decrypt the PII properties using the subject's encryption key.

For a full explanation of how to annotate types, identify the subject, and honor erasure requests, see the [Chronicle compliance client guide](xref:Chronicle.Compliance).

Arc provides an interceptor that handles PII decryption transparently.

## Example

Given a read model with a PII-annotated property:

```csharp
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string CompanyName,
    [PII] string ContactEmail);
```

All query endpoints that return `CustomerProfile` automatically serves decrypted values, without any changes to the query methods themselves.

## Behavior on Failure

`Release()` is intentionally non-breaking:

- If the read model has **no PII-annotated properties**, the original instance is returned immediately without contacting the server.
- If the encryption key **no longer exists** (e.g. after a right-to-erasure request), the original encrypted instance is returned and an error is logged. No exception is thrown.
