# Availability manifest and test vectors

This folder is the contract every Arc implementation (.NET today, Elixir and others next) shares for availability.
It holds the manifest file format, its JSON schema and the data-driven test vectors that any engine runs.

- [`manifest.schema.json`](./manifest.schema.json): the schema of the manifest file, version 1.
- [`vectors/`](./vectors): cases that an engine runs against a manifest. The .NET engine runs them in
  `Source/DotNET/Arc.Availability.Specs/for_ManifestFileEvaluator`.

The full description of the format and of how to run the vectors is in the Arc documentation:
`Documentation/backend/csharp/availability-engine/manifest-file.md`.

## Running the vectors in another engine

Each file in `vectors/` is `{ format: "cratis-availability-vectors", version: 1, name, manifest, cases }`. For every case:

1. Take the `part` by full name from `manifest.parts`. A part the manifest does not list is available.
2. Build the context: `context.tenant`, `context.user`, `context.audiences` (the audiences held), `context.suspended` (the
   paths that are suspended), `context.values` (further context values by name) and the case's `arguments`.
3. Decide, and compare with `expect`: `available`, and when it is not available the `reason` (`NotAvailable` or `Suspended`).

Decide in this order, which is the contract:

1. **Suspension first.** The part path is the module, feature and slice of the part that are not empty, followed by the full
   name of the part, all joined with `/`. A suspended path covers a part when it equals the part path, is a whole-segment
   prefix of it (`path + "/"`), or equals the full name of the part. A covered part is `Suspended`, whatever its gates say.
2. **Gates down the path.** Every gate in `part.gates` must hold. No gates means available.
3. **Three-valued logic.** An expression is available, unavailable or unknown. Only available opens a gate; unknown and
   unavailable are both `NotAvailable`. A value that is absent is unknown, for every operator, including `notEqual` and `notIn`.
   `all` is unavailable when any item is unavailable, otherwise unknown when any item is unknown. `any` is available when any
   item is available, otherwise unknown when any item is unknown. `not` over an audience is unknown when the audiences
   could not be decided; here they always can.
4. **Context paths.** `tenant`, `identity.user`, `arguments.<name>` and `<name>.<path>` (from `context.values`) are read from
   the context. The tenant and the identity always come from the server and are never replaced by arguments.

Run every file in the folder, and fail when none is found or no case runs.

## Changing the contract

Change the format and the vectors here first, then every implementation. Bump `version` for a change an existing reader
would misread. Never edit a pinned copy of a vector to make an implementation pass.
