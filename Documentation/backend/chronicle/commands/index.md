---
uid: Arc.Chronicle.Commands
---
# Commands

Chronicle extends the [Arc command pipeline](../../commands/index.md) with event sourcing behavior. The same [model-bound](../../commands/model-bound/index.md) command patterns work here — `[Command]` records, `Handle()` methods, `CommandValidator` — with Chronicle adding automatic event appending, event source identity resolution, and metadata-driven concurrency control.

## What Chronicle Adds

A model-bound command handler in the Chronicle context can return events directly from `Handle()`, and Chronicle appends those events to the event log automatically. This keeps command handlers focused on decisions and domain logic rather than event log plumbing.

Chronicle also resolves the event source identity, event stream metadata, and concurrency scope from the command record itself — either by convention or via explicit attributes and interfaces. This means the same record that defines your command's shape also carries all the information Chronicle needs to append events correctly.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Events](./events.md) | Returning events from commands and how Chronicle appends them automatically, including event source id resolution and stream metadata. |
| [Concurrency](./concurrency.md) | Declaring a concurrency scope on commands using metadata attributes and interfaces. |
