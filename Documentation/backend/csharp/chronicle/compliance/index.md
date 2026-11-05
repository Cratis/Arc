---
title: Cratis Arc compliance modeling
description: Model personal data with Cratis Arc and Chronicle by choosing an encryption subject and releasing protected values on supported query paths.
---

Cratis Arc integrates Cratis Chronicle's personal-data protection into command handling and supported query responses. Use it to choose whose key protects personal values and to decrypt those values when serving an authorized reader. These mechanisms support an erasure workflow; they do not make an application automatically compliant with a regulation.

Chronicle encrypts properties marked as personally identifiable information (`[PII]`) at the event log boundary, using a key for the subject the data belongs to. Deleting that key, called crypto-shredding, makes values encrypted under it unreadable unless another key copy was retained. The events remain, so the system can keep its business history without retaining readable protected values.

Your application still chooses the subject, authorizes access, and handles privacy and retention outside that protection. Key deletion does not erase historical plaintext, causation metadata, exports, or independently retained copies.

## What that looks like in practice

Model the write and read sides together:

- Choose the encryption subject for events. Arc can supply command metadata for events returned by a handler; aggregate `Apply()` does not forward it. Without an explicit append subject, Chronicle checks the event's subject metadata, then falls back to the event source id. Follow [Subject](./subject.md) to keep a customer's personal data separate from an order's identity.
- Mark the read model's personal values and give it a matching subject. Arc's read-model interception requests release, Chronicle's operation for decrypting protected values, before supported query responses reach the client. This includes supported streaming emissions and observable HTTP snapshots. Follow [PII](./pii.md) for the model shape and the [interception coverage table](../../queries/read-model-interception.md) for response-path boundaries.

Read models injected into commands have a separate release path. For stored read-model dependencies, Arc requests release whenever an instance exists, even if the command has no declared subject. Release resolves identity from the model, not the command. Materialized state may already have been released by Chronicle, while a passive reducer can build state from events in-process. The [Subject](./subject.md) guide explains why those paths need separate verification.

:::caution[Erasure and release failures are different]
Chronicle releases genuinely encrypted string values with an erased key as empty strings. A service error response can leave the original instance unchanged; transport or deserialization failures can throw. Verify both the erased-value shape and operational error handling before relying on a response.
:::

## Topics

| Topic | Description |
| --- | --- |
| [PII](./pii.md) | Model protected read-model values and understand release behavior before exposing a query. |
| [Subject](./subject.md) | Route event encryption to the right identity and match it when releasing a read model. |

## How Chronicle compliance works underneath

Chronicle uses the read model's own schema to encrypt projected personal values for storage. Each read model needs its own PII annotations, directly on properties or through shared concepts; receiving a value from an annotated event does not supply those annotations. Release uses that model's schema and subject to decrypt its protected values.

Continue with the [Chronicle compliance guide](/chronicle/compliance/) to annotate event types, manage encryption keys, and plan the wider erasure workflow.
