# Chronicle

`Cratis.Arc.Chronicle` is the integration package that extends Arc with [Cratis Chronicle](https://github.com/Cratis/Chronicle) capabilities. It wires the two frameworks together so that Arc's application model — commands, queries, identity, tenancy, and code generation — works seamlessly with Chronicle's event sourcing infrastructure.

Arc does not require Chronicle. Start with Arc over MongoDB or EF Core when current-state persistence is enough; add this integration when history, auditability, replay, reactors, or aggregate streams become part of the problem.

## What it provides

Without this package, Arc and Chronicle are independent. With it:

- **Commands return events** — `Handle()` methods on commands can return event records directly; the package appends them to the correct event log automatically.
- **Event source resolution** — the command context (current user identity, tenant, route parameters) is used to resolve the event source id without manual plumbing.
- **Read models backed by projections** — Arc's read model conventions drive Chronicle projections so that query responses always reflect the current projected state.
- **Tenant-aware event stores** — each tenant's event log and projections are namespaced automatically, matching Arc's tenancy model.
- **Compliance integration** — PII-annotated properties are decrypted transparently before read models are served, and the compliance subject is set on commands from the current identity.
- **Aggregate support** — aggregate roots are discoverable and invocable via the standard command pipeline, with Chronicle managing the event stream and rehydration.
- **Validation with read models** — domain constraint checks can read current projected state directly inside `Handle()` without an extra query round-trip.

## Topics

| Topic | Description |
| ----- | ----------- |
| [Aggregates](aggregates/index.md) | Working with aggregate roots and event sourcing. |
| [Add event sourcing to an Arc slice](add-event-sourcing.md) | Move one database-backed slice to Chronicle while keeping its query and React screen in place. |
| [Cratis Package](cratis-package.md) | The convenience package for Arc + Chronicle applications. |
| [React to an event](react-to-an-event.md) | Run side effects or follow-up commands from Chronicle events with reactors. |
| [Commands](commands/index.md) | Returning events from commands, event source id resolution, and concurrency scoping. |
| [Resolving EventSourceId](resolving-event-source-id.md) | How Chronicle resolves aggregate and read model identity from commands and query arguments. |
| [Read Models](read-models.md) | How read models are hooked up to Chronicle projections. |
| [Tenancy](tenancy.md) | Tenant-aware namespaces for event stores and projections. |
| [Validation](validation.md) | Validation with read models and identity resolution conventions. |
| [Compliance](compliance/index.md) | PII decryption on read models and compliance subject resolution on commands. |
| [Code Analysis](code-analysis/index.md) | Diagnostics and analyzers specific to the Chronicle integration. |
