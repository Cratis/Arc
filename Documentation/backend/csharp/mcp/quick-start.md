---
title: "Arc MCP quick start: expose a command and a query"
description: Expose one Cratis Arc command and one query as MCP tools, authorize a client with a personal access token, and call the tools in about ten minutes.
---

This page exposes one command and one query as MCP tools, authorizes a client with a personal access token (PAT), and calls the tools.

## Before you start

- An Arc application with [capability hosting](../capability-hosting.md) and a verified caller installation. This page uses the [PAT installation](../asp-net-core/personal-access-token-adapters.md#compose-browser-issuance-and-pat-callers), which needs the three PAT adapters (`IPatAuthority`, `IPatRecoveryEpoch`, `IPatPolicy`) registered. For local development you can use the [development adapters](#local-development-adapters) instead.
- A way to issue a PAT to a signed-in person. Arc does not provide an issuance endpoint; `IPersonalAccessTokens.Issue` is what your own endpoint calls.
- The `Cratis.Arc.Mcp` package referenced by the host.

## 1. Mark a command and a query

Put `[ExposeOverMcp]` on the command class and on a read model query method. The name is explicit and must match `^[a-z][a-z0-9_]{0,63}$`. The description is required.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Mcp;
using Cratis.Arc.Queries.ModelBound;

[Command]
[ExposeOverMcp("add_note", Description = "Adds a note.")]
public record AddNote(string Text)
{
    public void Handle() { /* your handler */ }
}

[ReadModel]
public record NoteReadModel(string Text)
{
    [ExposeOverMcp("list_notes", Description = "Lists notes.")]
    public static IEnumerable<NoteReadModel> All() => [];
}
```

Put `[Description("...")]` (`System.ComponentModel`) on a command property or a query parameter to give the agent a description of that argument. It appears as `description` in the tool's input schema; undescribed arguments get none.

The command must have a handler. You can declare the same tools in code instead: `Expose<AddNote>("add_note", "Adds a note.")` and `ExposeQuery("Notes.NoteReadModel.All", "list_notes", "Lists notes.")`, where the query name is the read model's full type name, a dot, and the method name.

## 2. Turn MCP on

```csharp
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", capabilityPort),
    configureBuilder: arc => arc
        .WithCapabilities(o => o.GatedEndpoints = new CapabilityEndpointSelector())
        .WithPersonalAccessTokens(
            new PatOptions("notes") { AllowedScopes = ["notes:read", "notes:write"] },
            callers => callers
                .ForResource("https://notes.example.test", "notes", "production")
                .UsePersonDirectory<PersonDirectory>()
                /* signed forwarding, as in the PAT adapter guide */
                .AllowPersonalAccessTokenCallers())
        .WithMcp(mcp => mcp.Configure(options =>
        {
            options.ApplicationName = "notes";
            options.PublicOrigin = "https://notes.example.test";
        })));

var app = builder.Build();
app.UseCratisArc();
app.MapCratisArcMcp();
```

What this sets:

- `ApplicationName = "notes"` makes the read scope `notes:read` and the write scope `notes:write`.
- `PatOptions.AllowedScopes` must list both scopes. A PAT can only hold scopes on that list, so without them no token can call the tools.
- The endpoint is `/mcp` on the capabilities port. Bind both the default listener and a listener on that port; see [Turn it on](advanced.md#turn-it-on).

If startup fails, check [Troubleshooting](troubleshooting.md).

### Local development adapters

The PAT module refuses issuance and verification until `IPatAuthority`, `IPatRecoveryEpoch` and `IPatPolicy` are registered. In a Development environment, `UseDevelopmentPersonalAccessTokenAdapters()` (namespace `Cratis.Arc.PersonalAccessTokens.Development`) registers process-local versions of all three, so you do not write them yet:

```csharp
arc.WithPersonalAccessTokens(/* as above */)
   .UseDevelopmentPersonalAccessTokenAdapters();
```

These adapters are for local development only:

- Credentials live in memory and disappear when the process restarts.
- Host startup throws `InvalidOperationException` outside the Development environment, and each adapter also refuses to work there. A warning is logged when they are selected.
- Do not combine them with production adapters. Production needs durable storage, product policy and recovery evidence.
- They replace the adapters only. Issuing a token still goes through `IPersonalAccessTokens.Issue`, which needs a signed-in browser caller.

## 3. Issue a PAT

From your own issuance endpoint, call `IPersonalAccessTokens.Issue(lifetime, scopes, cancellationToken)` for a signed-in person, with the scopes `notes:read` and `notes:write`. The result's `Token.Reveal()` returns the token once; store it only in the client. The maximum lifetime is 90 days.

## 4. Call the tools

Clients send the PAT as `Authorization: Bearer <token>`. To check the endpoint from a shell, list the tools:

```bash
curl -s https://notes.example.test:<capabilityPort>/mcp \
  -H "Authorization: Bearer $PAT" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

The response lists `add_note` and `list_notes`. A token that holds only `notes:read` sees only `list_notes`.

To call a tool, send `tools/call` with `{"name":"add_note","arguments":{"text":"hello"}}`. A command's arguments are its properties, named as Arc's JSON options name them; the `inputSchema` in the `tools/list` response shows the exact names and rejects anything else. The result carries the same JSON twice: as text content, and as `structuredContent` that matches the tool's `outputSchema`. It holds an `outcome` (`isSuccess`, `isAuthorized`, `isValid`, `validationResults`) and, for a successful call, `response` (commands) or `data` and `paging` (queries). Validation failures and refusals come back as a tool error, not as an HTTP error.

Next: [connect a real client](connect-clients.md) or [test with MCP Inspector](test-with-inspector.md).
