---
title: Compliance
description: PII is encrypted at the event log boundary and decrypted transparently on the way out, so commands and queries stay unaware of it.
---

Event sourcing and the right to erasure look like a contradiction. Events are immutable facts — that is the whole point — and yet someone can demand their personal data be deleted. You cannot rewrite history, but you must be able to make personal data unreadable.

Chronicle resolves this by encrypting `[PII]`-annotated properties at the event log boundary with a **per-subject key**. Crypto-shredding destroys that key, making values actually encrypted under it unreadable without another retained key copy. The events remain. This does not erase historical plaintext, causation metadata, exports, or independently retained copies; those need their own privacy and retention controls.

Arc integrates compliance release into read-model interception so supported query paths do not need handwritten decryption or key-fetching code. Your application still chooses the right subject and authorizes access to the data.

## What that looks like in practice

Two things have to happen, and both are automatic:

- **On the way in**, choose the compliance _subject_ — the identity whose key encrypts the data. Arc can supply command metadata for return-driven events; aggregate `Apply()` does not forward it. Absent an explicit append subject, Chronicle consults event subject metadata and finally the event source id. See [Subject](./subject.md).
- **On the way out**, read-model interception requests release before supported query responses and streaming emissions reach the client. Observable HTTP snapshots currently bypass interception; do not assume every transport releases PII identically. See [PII](./pii.md) and the [interception coverage table](../../queries/read-model-interception.md).

Command dependencies have their own release path. Materialized state may already be released by Chronicle; Arc conditionally requests an additional release when the command context has a subject, but does not pass that subject to the release call. Passive reducer state can be constructed in-process. See [Subject](./subject.md) before assuming these paths are equivalent.

:::caution[Erasure and release failures are different]
Chronicle releases genuinely encrypted values with an erased key as empty strings. Service error responses can retain the original instance, while transport or deserialization failures can throw. Test the resulting model and response shape; there is no unconditional non-breaking guarantee.
:::

## Topics

| Topic | Description |
| --- | --- |
| [PII](./pii.md) | Automatic decryption of PII-annotated properties on read models before they are served to clients. |
| [Subject](./subject.md) | Setting the compliance subject on a command so Chronicle keys PII encryption to the correct identity. |

## How Chronicle compliance works underneath

Chronicle encrypts properties annotated with `[PII]` at the event log boundary using a per-subject encryption key. The _subject_ is the compliance identity — typically a person rather than an aggregate. When events are projected into read models, encrypted values are stored as-is. Before a read model reaches a client, those values are decrypted with the subject's key.

For the full explanation of annotating types, managing encryption keys, and honoring erasure requests, see the [Chronicle compliance guide](/chronicle/compliance/).
