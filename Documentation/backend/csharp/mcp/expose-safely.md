---
title: "Expose an Arc command or query over MCP safely"
description: Choose which Cratis Arc commands and queries become MCP tools, and use opt-in exposure, scopes and the startup ceiling to keep agents inside safe limits.
---

Arc MCP exposes only what you declare. This page covers the choices that keep an exposed tool safe.

## Declare each tool explicitly

There are two ways to expose an operation, and both give the tool an explicit name and description:

| Way | Where | Example |
|---|---|---|
| Attribute | Command class, or read model query method | `[ExposeOverMcp("add_note", Description = "Adds a note.")]` |
| Builder | `WithMcp(mcp => ...)` | `.Expose<AddNote>("add_note", "Adds a note.")` and `.ExposeQuery("Notes.NoteReadModel.All", "list_notes", "Lists notes.")` |

The attribute is not inherited and can be applied once. Only commands that have a handler and read model query methods are discovered. Names are never derived from the CLR name, so renaming a class does not rename the tool.

## What the ceiling enforces

Arc checks every tool when the application starts. Each failure below throws an `InvalidMcpConfiguration` exception, so an unsafe tool never reaches a client.

| Rule | Error |
|---|---|
| Name matches `^[a-z][a-z0-9_]{0,63}$` | `McpToolNameInvalid` |
| Names are unique | `McpToolNameDuplicated` |
| Description present | `McpToolDescriptionMissing` |
| Operation is a known command or query | `McpExposedOperationUnknown` |
| Command does not require approval (`[RequiresApproval]`, inherited) | `McpApprovalRequiredCommandExposed` |
| Tool requires at least one non-blank scope | `McpToolHasNoScope`, `McpToolScopeBlank` |
| A tool that may change state requires the write scope | `McpWriteToolLacksWriteScope` |
| Name is not also used by a tool registered on the MCP server | `McpToolNameTakenByServerTool` |

## Pick the right operations

- Expose queries before commands. A query tool is annotated read-only and idempotent; a command tool is annotated as destructive and not idempotent, so clients know to ask before calling it.
- Expose a command only if an agent acting for a user should be able to do what the command does. The command's own authorization still applies, but exposing it is a product decision.
- Do not expose commands that need approval. Arc refuses them.
- Streaming (observable) queries are not available as tools; a call returns the tool error "The tool is not available on this host."

## Layer extra requirements

The read and write scopes are the minimum. Add `[RequiresScope("...")]` on a command type, handler, read model or query method to require more; the caller must hold all of them. For rules a scope cannot express, register an `IMcpToolAuthorizer` with `AddToolAuthorizer<T>()`; see [tool sources and authorizers](advanced.md#tool-authorizers). Authorizers also filter `tools/list`, so an agent never sees a tool it cannot call.

## Keep the endpoint narrow

- Leave `ExclusiveTools` at its default of `true`. Setting it to `false` serves tools registered directly on the MCP server without the Arc ceiling, and Arc logs a startup warning.
- Leave `Profile` at `PatOnly` unless you run your own OAuth issuer; see [Connect clients](connect-clients.md).
- Keep `AllowedOrigins` empty unless a browser client needs it. See [Test with MCP Inspector](test-with-inspector.md).
- Keep `MaxRequestBodySize` at the 1 MiB default unless tools take large input.

Next: [Scopes and descriptions](scopes-and-descriptions.md).
