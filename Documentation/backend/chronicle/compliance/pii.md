---
title: PII
description: Release encrypted read-model values through Arc's interception pipeline, with explicit subject, transport, and erasure boundaries.
---

When a query serves encrypted personal data, the caller needs usable values—not encryption plumbing in every query method. Arc's [read-model interception](../../queries/read-model-interception.md) integrates Chronicle's release operation into supported query responses and WebSocket/SSE emissions.

Observable HTTP snapshots currently bypass interception, including requests using `waitForFirstResult=true`. Choose and authorize each exposed response path deliberately; consult the interception coverage table rather than assuming a streaming guarantee also covers snapshots.

For the full Chronicle-level guide on annotating types, identifying subjects, and honoring erasure requests, see [Chronicle compliance](/chronicle/compliance/).

## How It Works

Chronicle encrypts `[PII]` properties at the event log boundary under the subject's encryption key. On intercepted paths, Arc calls `Release()` on `IReadModels`, keeping decryption out of individual query methods. Release is not authorization, and it cannot restore an erased key.

Given a read model with a PII-annotated property:

```csharp
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string CompanyName,
    [PII] string ContactEmail);
```

This is a model fragment; import `Cratis.Chronicle` and `Cratis.Chronicle.Compliance.GDPR` and supply the application's `CustomerId` concept. Query paths using Arc's read-model interception attempt release automatically. The instance's subject must match the encryption subject, and release is not an authorization check.

## Behavior on Failure

Distinguish erasure from an operational failure:

- With no compliance metadata, the client returns the original instance without a release request. With no resolvable subject, it logs and returns the instance.
- The server passes through values it does not recognize as encrypted. This is not retroactive encryption of historical plaintext.
- For a genuinely encrypted value whose key was erased, the server releases an **empty string**, not the original ciphertext.
- An error returned by the compliance service is logged and the client retains the original instance.
- Transport, schema generation, decryption, or deserialization can still throw. In particular, test how empty released strings behave with your non-string concepts and converters.

Model the absence of erased values deliberately. Test response shapes and exception handling through the client and server together; successful compilation alone does not exercise release. See [Subject](./subject.md) for materialized and passive dependency paths.

## Audit exclusion is a different boundary

Command `[PII]` and `[NotAudited]` annotations can exclude command values from [causation](../commands/causation.md). A command annotation does not automatically annotate a newly constructed event. To encrypt persisted personal data, annotate the event's property/type or a shared concept used by that event. Neither attribute is a substitute for access control or removing secrets from nested audit payloads.
