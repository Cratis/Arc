---
uid: Arc.Chronicle.Compliance
---
# Compliance

Arc integrates with Chronicle's compliance infrastructure to handle PII (Personal Identifiable Information) transparently across the full stack — from event appends through to query responses.

## Topics

| Topic | Description |
| ----- | ----------- |
| [PII](./pii.md) | Automatic decryption of PII-annotated properties on read models before they are served to clients. |
| [Subject](./subject.md) | Setting the compliance subject on a command so Chronicle keys PII encryption to the correct identity. |

## How Chronicle Compliance Works

Chronicle encrypts properties annotated with `[PII]` at the event log boundary using a per-subject encryption key. The *subject* is the compliance identity — typically a person rather than an aggregate. When events are projected into read models, encrypted values are stored as-is. Before presenting a read model to a client, the encrypted values must be decrypted using the subject's key.

For a full explanation of how to annotate types, manage encryption keys, and honor erasure requests, see the [Chronicle compliance client guide](xref:Chronicle.Compliance).
