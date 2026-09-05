---
title: Compliance
description: PII is encrypted at the event log boundary and decrypted transparently on the way out, so commands and queries stay unaware of it.
---

Event sourcing and the right to erasure look like a contradiction. Events are immutable facts — that is the whole point — and yet someone can demand their personal data be deleted. You cannot rewrite history, but you must be able to make personal data unreadable.

Chronicle resolves this by encrypting `[PII]`-annotated properties at the event log boundary with a **per-subject key**. Erasure then means destroying that one key: the events stay exactly where they are, and the personal data inside them becomes permanently unrecoverable. The history is intact, the person is forgotten.

Arc's job is to make this invisible to your code. You never encrypt, never decrypt, never fetch a key.

## What that looks like in practice

Two things have to happen, and both are automatic:

- **On the way in**, the command has to carry the compliance *subject* — the identity whose key encrypts the data. Arc resolves it from the command and puts it in the command context. See [Subject](./subject.md).
- **On the way out**, read models have to be decrypted before a client sees them. Arc's read model interception pipeline does this for every query type — controller-based, model-bound, and observable. See [PII](./pii.md).

The same release happens for a read model injected into a command, so a validator sees decrypted values under the same identity that encrypted them.

:::note[Decryption never breaks a response]
If the key is gone — after an erasure request — the encrypted value is returned as-is and the failure is logged. Queries keep working; the data is simply unreadable, which is exactly what erasure means.
:::

## Topics

| Topic | Description |
| ----- | ----------- |
| [PII](./pii.md) | Automatic decryption of PII-annotated properties on read models before they are served to clients. |
| [Subject](./subject.md) | Setting the compliance subject on a command so Chronicle keys PII encryption to the correct identity. |

## How Chronicle compliance works underneath

Chronicle encrypts properties annotated with `[PII]` at the event log boundary using a per-subject encryption key. The *subject* is the compliance identity — typically a person rather than an aggregate. When events are projected into read models, encrypted values are stored as-is. Before a read model reaches a client, those values are decrypted with the subject's key.

For the full explanation of annotating types, managing encryption keys, and honoring erasure requests, see the [Chronicle compliance guide](/chronicle/compliance/).
