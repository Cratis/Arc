---
title: "Arc MCP capability manifest: one allowlist for MCP and REST"
description: The versioned screenplay-capability-manifest/1 allowlist names the operations agents may reach, with scopes and descriptions; Cratis Arc fails closed on mismatch.
---

A capability manifest is a versioned JSON allowlist of what agents may reach in an application: which operations, whether each reads or writes, the scopes it requires, its description, and whether it requires approval. The format identifier is `screenplay-capability-manifest/1`.

The manifest exists so that what a model declares for agents and what a running application exposes cannot drift apart. One artifact feeds the MCP tools and the `/v1` REST surface.

<!-- TODO(L110): Cratis/Arc#3750 (manifest contract) and #3751 are not implemented in the branch: no code in Source/ mentions the manifest, so nothing below the "Contract" heading is verified. Fill in from the delivered code: the JSON schema and a real example, where the file is configured (option or builder method), how the manifest is loaded, and the exact error names and messages. The text below states the issue's requirements, not shipped behavior. -->

## What it contains

Per operation:

| Field | Meaning |
|---|---|
| Operation | The command or query it allows. |
| Kind | Query or command, read or write. |
| Scopes | The scopes a caller must hold. |
| Description | What the agent is told about the tool. |
| Approval requirement | Whether the operation requires approval. |

## How Arc uses it

Arc builds the MCP tool surface and the `/v1` REST surface from the manifest. An operation that is not listed is not served on either surface. The checks in [Expose safely](expose-safely.md) still apply: names, descriptions, write scopes and the approval rule are validated at startup.

## Fail closed

Startup fails when the manifest and the registered operations disagree. The disagreements the contract names:

- an operation is registered but not listed in the manifest;
- a listed operation has no description;
- a listed operation requires a scope that is unknown to the application.

A mismatch never falls back to exposing everything. The host does not start, and no endpoint is served from a partial manifest. A malformed or unsupported-version manifest is also refused.

## Contract

<!-- TODO(L110): replace this section with the real schema and a worked example once Arc consumes the manifest. Verify against Screenplay emission (Cratis/Screenplay#1199). -->

## Related

- [Expose safely](expose-safely.md)
- [Scopes and descriptions](scopes-and-descriptions.md)
- [Troubleshooting](troubleshooting.md)
