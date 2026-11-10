---
title: "Arc MCP tool descriptions, scopes and PAT scopes"
description: Write MCP tool descriptions agents can use, set the read and write scopes in Cratis Arc, and allow those scopes on personal access tokens.
---

An MCP tool has a name, a description, and the scopes it requires. The agent reads the description to decide when to call the tool. The scopes decide who can see and call it.

## Write descriptions for an agent

The description is the only guidance the agent gets, and it is required. Say what the tool does, what it needs and what it returns:

```csharp
[ExposeOverMcp("list_notes", Description = "Lists the caller's notes, newest first. Use page_size to limit the result.")]
```

- State the effect of a command in one sentence ("Adds a note and returns its id.").
- Name inputs the agent must get right.
- Do not include secrets, internal names or instructions to the agent about unrelated tools.

Describe arguments too. `[Description("...")]` (`System.ComponentModel`) on a command property or a query method parameter becomes the `description` of that argument in the tool's input schema:

```csharp
[ExposeOverMcp("add_note", Description = "Adds a note and returns its id.")]
public record AddNote([property: Description("The note text, plain text.")] string Text);
```

Arc does not infer argument descriptions from names or XML comments; an argument without the attribute has none.

For query tools, the parameter schema is generated from the query parameters. If the query supports paging, the tool also accepts `page` (0 or more) and `page_size` (1 or more); paging applies only when `page_size` is given.

## Read and write scopes

| Tool | Always requires |
|---|---|
| Query | `ReadScope` |
| Command | `WriteScope` |

The defaults are `<ApplicationName>:read` and `<ApplicationName>:write`. Set `ReadScope` and `WriteScope` to use your own names. With no application name and no explicit scope, startup fails with `McpToolHasNoScope`.

```csharp
mcp.Configure(options =>
{
    options.ApplicationName = "notes";
    options.WriteScopes.Add("notes:publish");
});
```

`WriteScopes` lists additional write scopes. `WriteScope` is always a write scope. A scope in `WriteScopes` must not be blank, repeated or equal to `ReadScope`; otherwise startup fails with `McpOptionsInvalid`.

Add more required scopes with `[RequiresScope("notes:admin-read")]` from `Cratis.Arc.Authorization`. The attribute can be repeated, and the caller must hold every one. For a command it goes on the command type, a `Handle` method or a handler parameter. For a query it goes on the read model type, the performer type or the query method.

## Allow the scopes on tokens

A personal access token can only hold scopes listed in `PatOptions.AllowedScopes`. The list is empty by default, which allows none. It is an explicit, case-sensitive list: no `*`, and no scope with an `admin` or `approve` segment.

```csharp
new PatOptions("notes") { AllowedScopes = ["notes:read", "notes:write"] }
```

Every scope an MCP tool requires, including `[RequiresScope]` extras, must be on this list for a PAT to be able to call the tool. This is the most common setup mistake; see [Troubleshooting](troubleshooting.md).

## What the caller sees

- `tools/list` returns only tools whose scope set the caller holds and that the registered authorizers allow. A caller with no verified identity gets an empty list.
- A call to a tool outside the caller's scopes returns HTTP 403 `insufficient_scope`, with a `WWW-Authenticate` header that names the missing scopes.
- Protected resource metadata lists `ScopesSupported`, or the read and write scopes when you leave it empty.
