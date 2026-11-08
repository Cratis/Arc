---
title: "Serving MCP tools from Cratis Arc"
description: Expose commands and queries to MCP clients on the Cratis Arc capabilities port, with scoped access, a fixed tool ceiling, an Origin check, and protected resource metadata.
---

The `Cratis.Arc.Mcp` package serves a [Model Context Protocol](https://modelcontextprotocol.io) endpoint from your Arc application. MCP clients call the commands and queries you choose, as the verified caller the [capability gate](capability-hosting.md) admitted. Nothing is exposed unless you declare it.

## What it does

The endpoint is stateless: every request is one call that runs in the scope of its HTTP request. It is served only on the capabilities [endpoint group](endpoint-groups.md) port, behind the capability gate. A request on the default port is not served. Gate refusals use the MCP error shape, with `WWW-Authenticate` for authentication failures.

The tools a caller sees depend on that caller's scopes. A caller without the write scope never sees a write tool, and a call to a tool outside the caller's scopes is refused before it reaches the server.

## Turn it on

Capability hosting with a verified caller installation is required. Then add `WithMcp` and map the endpoint:

```csharp
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", capabilityPort),
    configureBuilder: arc => arc
        .WithCapabilities()
        .WithVerifiedCallers(callers => /* your installation */)
        .WithMcp(mcp => mcp
            .Configure(options =>
            {
                options.ApplicationName = "notes";
                options.PublicOrigin = "https://notes.example.test";
            })
            .Expose<AddNote>("add_note", "Adds a note.")
            .ExposeQuery("Notes.NoteReadModel.All", "list_notes", "Lists notes.")));

var app = builder.Build();
app.UseCratisArc();
app.MapCratisArcMcp();
```

You can also declare a tool next to the code with `[ExposeOverMcp("add_note", Description = "Adds a note.")]` on a command class or a read model query method. Tool names are explicit and stable; they are never derived from the CLR name. A tool without a description fails startup.

Query tools take the query parameters as arguments. Like the REST endpoints, paging applies only when `page_size` is given (with an optional `page`), and an argument value must be a scalar, or an array for a collection parameter. Streaming query results are not available as tools.

## Scopes

Read tools require the read scope and write tools require the write scope. By default they are `<ApplicationName>:read` and `<ApplicationName>:write`; set `ReadScope` and `WriteScope` to choose your own. If neither an application name nor an explicit scope is configured, startup fails with `McpToolHasNoScope` rather than exposing a tool that requires no scope.

## Curated tools with `IMcpToolSource`

Implement `IMcpToolSource` for a tool that is not a plain command or query, and register it with `AddToolSource<TSource>()`. The source describes its tools (name, description, required scopes, whether it reads or writes, and an input schema) and handles the call. `McpToolDescriptor.Custom(...)` builds a descriptor and picks the conservative hints for reads or writes:

```csharp
public class NoteTools : IMcpToolSource
{
    public IEnumerable<McpToolDescriptor> Describe() =>
    [
        McpToolDescriptor.Custom("count_notes", "Counts the notes.", ["notes:read"], McpToolAccess.Read)
    ];

    public ValueTask<McpToolResult> Invoke(McpToolInvocation invocation) =>
        ValueTask.FromResult(McpToolResult.Success("42"));
}
```

The ceiling checks every described tool at startup:

- The name and description must be valid, and names must be unique.
- The tool must require at least one non-blank scope.
- A tool that writes must require the write scope. Requiring only the read scope fails startup with `McpWriteToolLacksWriteScope`.

A curated tool can also accept alternative scope sets. A caller may call the tool when it holds every required scope, or every scope of any one alternative set:

```csharp
McpToolDescriptor.Custom("audit_log", "Reads the audit log.", ["notes:read"], McpToolAccess.Read) with
{
    AlternativeScopes = [["audit:read", "audit:export"]]
}
```

The same check decides what `tools/list` shows, what the HTTP admission lets through and what the dispatcher runs, so they always agree. An empty or blank alternative set fails startup, and for a tool that writes every alternative set must include the write scope. A caller that holds only part of a set is refused, and the `insufficient_scope` challenge names the required scopes.

An exception thrown by a tool is reported to the caller as a generic tool error. Its message is not returned.

## The ceiling

The set of tools is fixed at startup. Exposure, scopes, names, and input schemas are computed once into a frozen ceiling, and every part of the server reads it. A command that requires approval cannot be exposed. Callers cannot widen the ceiling through arguments, headers, or differently cased JSON-RPC members; members are matched strictly, and arguments naming a tenant or principal are refused.

## Tool authorizers

Scopes decide which tools a credential may use. For a decision scopes cannot express, such as a per-tenant plan or a feature flag, implement `IMcpToolAuthorizer` and register it with `AddToolAuthorizer<TAuthorizer>()`. Authorizers are resolved from the services of each request:

```csharp
public class PlanAuthorizer(IPlans plans) : IMcpToolAuthorizer
{
    public async ValueTask<bool> IsAllowed(McpToolAuthorization authorization) =>
        await plans.Includes(authorization.Caller.Tenant, authorization.Tool.Name);
}
```

Authorizers only narrow access:

- They run only for a caller whose scopes already satisfy the tool.
- Every registered authorizer must allow the tool. One that denies or throws hides the tool from `tools/list` and refuses the call with `403 forbidden` before the MCP server reads it. The dispatcher checks again before it runs the tool.
- They apply to the tools of the ceiling only, not to tools registered directly on the MCP server.

## Tools registered on the MCP server

Tools you register directly on the MCP server, for example with `AddMcpServer().WithTools(...)`, are served next to the ceiling. They are not part of the ceiling and Arc does not govern them:

- They are listed to every admitted caller and their calls reach the server without a scope check. Arc still admits the request first, so the caller is verified, the credential profile and the `Origin` check apply, and the body is bounded.
- Check scopes and any other authorization inside these tools yourself.
- A server tool whose name matches a ceiling tool, ignoring case, fails startup with `McpToolNameTakenByServerTool`, so it can never stand in for a ceiling tool.

Set `ExclusiveTools` to serve only the ceiling. Arc then removes the server's own tools, and any custom list or call handlers, from the server:

```csharp
mcp.Configure(options => options.ExclusiveTools = true);
```

## Credentials and protected resource metadata

`Profile` selects the accepted credentials, and the endpoint enforces it against the verified caller's credential kind:

| Profile | Accepts |
| --- | --- |
| `PatOnly` (default) | Personal access tokens. No metadata is advertised. |
| `OAuthOnly` | OAuth access tokens. |
| `Combined` | Both, each by its own verifier. |

A caller verified with any other credential kind, such as a browser session, is refused with the same `401` and `WWW-Authenticate` challenge as a request without a caller, and the tool does not run. The refusal says nothing about why. The default is `PatOnly`.

The OAuth and combined profiles serve [protected resource metadata](https://www.rfc-editor.org/rfc/rfc9728) so clients can discover the authorization server. Configure `Resource`, `PublicOrigin`, and at least one entry in `AuthorizationServers`. These values are never derived from the request host. Startup validates them:

- `PublicOrigin` must be an origin (no path, query, or fragment) and use `https`. Plain `http` is accepted only for loopback. This applies under every profile whenever `PublicOrigin` is set, including the personal access token profile.
- `Resource` must be `PublicOrigin` plus `Path`, with no fragment, query, or user info.

## Origin check

Browsers attach an `Origin` header to cross-site requests, which lets a web page call a local MCP endpoint. The endpoint therefore refuses a request whose `Origin` is not allowed, with `403`. A request without an `Origin` header, such as one from a command-line or desktop client, is allowed. The allowed set is `PublicOrigin` plus `AllowedOrigins`:

```csharp
mcp.Configure(options =>
{
    options.PublicOrigin = "https://notes.example.test";
    options.AllowedOrigins.Add("https://app.example.test");
});
```

Each entry must be an `https` origin (loopback may use `http`). Comparison ignores case. More than one `Origin` value, or an `Origin` that is not an origin (such as `null` or a URL with a path), is refused.

## Related

- [Capability hosting](capability-hosting.md)
- [Endpoint groups](endpoint-groups.md)
- [Verified callers](asp-net-core/verified-callers.md)
