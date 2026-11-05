---
title: Cratis Arc PII release
description: Cratis Arc releases encrypted personal values through read-model interception. Model the subject and handle erased values and release failures.
---

Cratis Arc uses read-model interception to release, or decrypt, personally identifiable information (PII) protected by Cratis Chronicle before supported query responses reach the caller. Annotate the read model and identify the subject whose key encrypted its values, rather than writing decryption code in each query method.

Release applies to intercepted query responses, supported WebSocket/SSE emissions, and observable HTTP snapshots, including requests using `waitForFirstResult=true`. Check the [read-model interception coverage table](../../queries/read-model-interception.md) for the paths your application exposes. Interception does not authorize access; select only the rows and fields the caller may receive.

For event annotations, key management, and the wider erasure workflow, continue with [Chronicle compliance](/chronicle/compliance/). Encryption and release are mechanisms for that workflow, not automatic regulatory compliance.

## How It Works

Chronicle encrypts `[PII]` properties at the event log boundary under the subject's encryption key. The read model needs its own PII annotations, either on its properties or on shared concepts; an event annotation does not automatically annotate a projection's output. On intercepted paths, Arc calls `Release()` on `IReadModels` using the read model's schema and subject.

In this customer-profile model, `[Subject]` identifies whose key protects `ContactEmail`, while `[PII]` identifies the value to release:

```csharp
public record CustomerProfile(
    [Subject] CustomerId CustomerId,
    string CompanyName,
    [PII] string ContactEmail);
```

This is a model fragment; import `Cratis.Chronicle` and `Cratis.Chronicle.Compliance.GDPR` and supply the application's `CustomerId` concept. `CustomerId` must resolve to the same subject used when encrypting the event value. Arc requests release automatically on intercepted paths, but release cannot restore an erased key.

## Behavior on Failure

An unchanged value, an erased value, and a failed request mean different things:

- A model without applicable schema metadata returns unchanged, without a release request.
- A PII-only model with no resolvable subject returns unchanged. The client logs when it finds a subject-scoped value without a subject.
- A value the server does not recognize as encrypted passes through unchanged. Adding an annotation does not encrypt historical plaintext.
- A genuinely encrypted string value whose key was erased releases as an empty string, not the original ciphertext.
- An error response from the compliance service is logged, and the client retains the original instance.
- A transport, schema-generation, decryption, or deserialization failure can throw.

Decide how the client should display an erased email address and how the application should handle a failed release. Exercise both through the client and server together. Empty released strings can behave differently with non-string concepts and converters, so successful compilation is not enough to verify the response shape. Follow [Subject](./subject.md) when the same model is also a materialized or passive command dependency.

## Audit exclusion is a different boundary

Command `[PII]` and `[NotAudited]` annotations can exclude command values from [causation](../commands/causation.md), the recorded context of what caused an event. A command annotation does not automatically annotate a newly constructed event. To encrypt persisted personal data, annotate the event's property/type or a shared concept used by that event. Keep access controls and secrets handling separate, including for nested audit payloads.
