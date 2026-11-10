---
title: "Arc MCP tool sources, authorizers and session authority"
description: Advanced Cratis Arc MCP serving: curated tool sources, tool authorizers, the tool ceiling, session authority, protected resource metadata and the Origin check.
---

The `Cratis.Arc.Mcp` package serves a [Model Context Protocol](https://modelcontextprotocol.io) endpoint from your Arc application. MCP clients call the commands and queries you choose, as the verified caller the [capability gate](../capability-hosting.md) admitted. Nothing is exposed unless you declare it.

## What it does

The endpoint is stateless: every request is one call that runs in the scope of its HTTP request. It is served only on the capabilities [endpoint group](../endpoint-groups.md) port, behind the capability gate. A request on the default port is not served. Gate refusals use the MCP error shape, with `WWW-Authenticate` for authentication failures.

The tools a caller sees depend on that caller's scopes. A caller without the write scope never sees a write tool, and a call to a tool outside the caller's scopes is refused before it reaches the server.

## Turn it on

Capability hosting with a verified caller installation is required. Bind both the default public listener and a separate listener on the assigned capabilities port, even when the host serves only `/mcp`. Assigning the port does not bind the listener; configure both through Kestrel or `UseUrls`, as shown in [capability hosting](../capability-hosting.md). For a guided first setup, see the [quick start](quick-start.md).

Before either listener starts, Arc validates authorization declarations for every discovered command and query, not just the MCP ceiling. Missing or invalid authorization policies can therefore fail startup even for commands you have not exposed as tools.

Then add `WithMcp` and map the endpoint:

```csharp
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", capabilityPort),
    configureBuilder: arc => arc
        .WithCapabilities(o => o.GatedEndpoints = new CapabilityEndpointSelector())
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

An application with several areas often has more than one write scope. List the others in `WriteScopes`; `WriteScope` is always a write scope too, so an empty list means `WriteScope` alone:

```csharp
mcp.Configure(options =>
{
    options.ApplicationName = "notes";
    options.WriteScopes.Add("notes:publish");
    options.WriteScopes.Add("archive:write");
});
```

Commands exposed with `Expose` or `[ExposeOverMcp]` require `WriteScope`. A blank or repeated entry in `WriteScopes`, or an entry equal to `ReadScope`, fails startup with `McpOptionsInvalid`, so a read-only grant never holds a write scope. Protected resource metadata lists the read scope and every write scope unless you set `ScopesSupported` yourself.

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
- A tool that writes must require a write scope: `WriteScope` or one of `WriteScopes`. Requiring only the read scope, or only scopes that are not write scopes, fails startup with `McpWriteToolLacksWriteScope`.

A curated tool can also accept alternative scope sets. A caller may call the tool when it holds every required scope, or every scope of any one alternative set:

```csharp
McpToolDescriptor.Custom("audit_log", "Reads the audit log.", ["notes:read"], McpToolAccess.Read) with
{
    AlternativeScopes = [["audit:read", "audit:export"]]
}
```

The same check decides what `tools/list` shows, what the HTTP admission lets through and what the dispatcher runs, so they always agree. An empty or blank alternative set fails startup, and for a tool that writes every alternative set must include a write scope. One alternative without a write scope fails startup, because a caller holding only that set could otherwise change state. A caller that holds only part of a set is refused, and the `insufficient_scope` challenge names the required scopes.

An exception thrown by a tool is reported to the caller as a generic tool error. Its message is not returned.

### Rich results

A curated tool can return more than text. Set `Title` and `OutputSchema` on the descriptor, and `StructuredContent` and `ResourceLinks` on the result:

```csharp
McpToolDescriptor.Custom("count_notes", "Counts notes.", ["notes:read"], McpToolAccess.Read) with
{
    Title = "Count notes",
    OutputSchema = """{"type":"object","properties":{"count":{"type":"integer"}}}"""
};

McpToolResult.Success("""{"count":2}""") with
{
    StructuredContent = JsonDocument.Parse("""{"count":2}""").RootElement.Clone(),
    ResourceLinks = [new McpResourceLink("notes://notes/1", "First note") { MimeType = "text/plain" }]
};
```

The text is sent first, followed by one `resource_link` block for each link. Keep the text a serialized copy of the structured result so clients without structured support see the same data. Arc checks the shape and fails closed:

- An output schema that is not a JSON schema of type `object` fails startup with `McpToolOutputSchemaInvalid`.
- A result is replaced by a generic tool error when its structured content is not a JSON object, when a tool with an output schema succeeds without structured content, or when a link has no name or no absolute URI.
- Arc does not validate structured content against the output schema.

### The protocol request

`McpToolInvocation.Request` is the MCP protocol request the call arrived in, for example to read the progress token in its `Params`. It is null when the tool is invoked outside a protocol request. Keep these limits in mind:

- The endpoint uses the stateless HTTP transport, so requests from the server to the client, such as sampling and elicitation, are not supported.
- Identity comes only from `invocation.Caller`. `Request.User` and `Request.Services` are not the verified caller; never read the caller, tenant or scopes from the request.
- Treat `Params` as untrusted input.

## Session authority (arc-mcp-authorization-v1)

`Mcp-Session-Id` carries no authority. Every HTTP request must authenticate its own credential and pass the current resource, environment, tenant, scope, membership policy, and tool authorization checks. Initialization never freezes those facts. Arc uses stateless HTTP: initialization does not issue a session ID, and a syntactically valid session header cannot restore a previous caller or substitute for a missing or different credential. After admission, the SDK rejects session headers as unsupported in stateless mode.

The PAT verifier performs authoritative credential, recovery-epoch, and policy reads on every request; it has no positive verifier cache. Revocation therefore takes effect on the next request after the authoritative adapter observes it, with no session or cache grace period. Product adapters must provide current authoritative state, including membership decisions.

A client delivers its credential to the intended MCP resource to authenticate that request. This is separate from token passthrough: a tool must not forward the client's credential to another resource. The transport supplies the admitted caller, not a reusable bearer token, to tool handlers.

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

`McpToolAuthorization.Operation` is `McpToolOperation.List` when checking list visibility and `McpToolOperation.Call` during call admission and dispatch. Contexts constructed without setting `Operation` default to `Call`.

Authorizers only narrow access:

- They run only for a caller whose scopes already satisfy the tool.
- Every registered authorizer must allow the tool. One that denies or throws for `List` hides the tool from `tools/list`; hiding is visibility only, not execution authorization. One that denies or throws for `Call` refuses the call. A client that calls a tool the authorizer denies for `Call` gets HTTP `403` with the body `{"error":"forbidden"}` and no `WWW-Authenticate` challenge, before the MCP server reads the request; it is not a tool error, and no reason is returned. The dispatcher checks again before it runs the tool.
- They apply to the tools of the ceiling only, not to tools registered directly on the MCP server.
- They run for every ceiling tool on every `tools/list`, and again for each call, in admission and in the dispatcher. Keep them cheap, and cache any lookup they need.

## Tools registered on the MCP server

By default only the ceiling is served. Tools you register directly on the MCP server, for example with `AddMcpServer().WithTools(...)`, are removed, together with any custom tool list or call handlers. To serve them next to the ceiling, turn `ExclusiveTools` off:

```csharp
mcp.Configure(options => options.ExclusiveTools = false);
```

Those tools are not part of the ceiling and Arc does not govern them, so Arc logs a warning at startup:

- They are listed to every admitted caller and their calls reach the server without a scope check. Arc still admits the request first, so the caller is verified, the credential profile and the `Origin` check apply, and the body is bounded.
- Check scopes and any other authorization inside these tools yourself.
- A server tool whose name matches a ceiling tool, ignoring case, fails startup with `McpToolNameTakenByServerTool`, so it can never stand in for a ceiling tool.
- The name check covers the server's tool collection only. Tools served by a custom `ListToolsHandler` or `CallToolHandler` are outside it; make sure their names never match a ceiling tool.

## What Arc owns of the MCP server

`WithMcp` calls `AddMcpServer` and configures the HTTP transport, and `MapCratisArcMcp` maps the endpoint. Adding your own `AddMcpServer()` call is fine: it returns a builder for the same server, so tools, prompts and resources you add through it are kept. Arc owns these parts; do not change them in your own configuration:

- the tool collection, which Arc sets after your configuration to your server tools plus the ceiling when `ExclusiveTools` is off, and only the ceiling otherwise;
- the `tools/list` filter and the stateless HTTP transport.

Arc governs tools only. Prompts and resources you add to the server are not filtered by scope or tool authorizers; every admitted caller can list and read them. Check access inside their handlers, or do not register them on a server that serves callers with different rights.

## Write a refusal from host middleware

Use `McpRefusals.Unauthorized(httpContext, options)`, `McpRefusals.Forbidden(httpContext)`, or `McpRefusals.InsufficientScope(httpContext, options, requiredScopes)` to write the same JSON refusals as Arc without copying their bodies. Await the returned task and stop processing the request. These methods only write a response; they do not verify credentials, create a caller, or bypass any authorization check. An `IMcpToolAuthorizer` must still return `false` to deny.

The responses are `401` with `{"error":"unauthorized"}`, `403` with `{"error":"forbidden"}`, and `403` with `{"error":"insufficient_scope"}` respectively. Unauthorized and insufficient-scope responses include the configured Bearer challenge. Pass the host's validated `McpOptions` and configured required scopes, not request-header values. For insufficient scope, set `stepUp: true` only for a verified OAuth caller that supports step-up; it defaults to `false`, so a PAT refusal does not advertise OAuth metadata.

## Extension points for transport packages

Optional transports can use these public seams without depending on Arc internals:

- `CapabilitiesServiceProviderExtensions.HasCapabilities()` checks host registration; `CapabilityEndpointSelector.HasEndpointMetadata<TMetadata>()` checks an exact gate rule.
- `CapabilityRequestContext.GetCaller()` and `GetVerifiedCaller()` read facts already admitted in the current request's item bag. They do not trust `HttpContext.User`. `SetProtocol()` labels an existing admitted caller without granting authority. Never reuse an item bag across requests.
- `OperationContextScope.Begin()` captures and restores receipt time only. It does not isolate identity, tenant, or correlation state; use the hosted pipelines and request-scoped services for those boundaries.
- `CommandPipeline.ExecuteHosted()` and `QueryPipeline.PerformHosted()` retain hosted authorization and identity-bound dependency scopes. Read an untyped command response with `CommandResult.ResponsePayload`. Materialize a query response before calling `QueryResult.ReleaseScope()` in a `finally` block.
- `ConverterExtensions.IsEnumerableOfQueryArgumentElement()` classifies collection arguments consistently with Arc query binding.

Use an Arc dependency version that includes these APIs. MCP's package dependency requires its corresponding Arc baseline; older Arc binaries that predate these extension points are not supported overrides. Compatible later Arc releases must preserve these public contracts.

## Credentials and protected resource metadata

`Profile` selects the accepted credentials, and the endpoint enforces it against the verified caller's credential kind:

| Profile | Accepts |
| --- | --- |
| `PatOnly` (default) | Personal access tokens. No metadata is advertised: the metadata route is still mapped and answers `404`. |
| `OAuthOnly` | OAuth access tokens. |
| `Combined` | Both, each by its own verifier. |

The MCP endpoint explicitly admits both PAT and verified OAuth bearer presentations through the capability gate; its profile then restricts the verified credential kind. Other capability-gated endpoints remain PAT-only when a PAT caller installation is present.

A caller verified with any other credential kind, such as a browser session, is refused with the same `401` and `WWW-Authenticate` challenge as a request without a caller, and the tool does not run. The refusal says nothing about why. The default is `PatOnly`.

The OAuth and combined profiles serve [protected resource metadata](https://www.rfc-editor.org/rfc/rfc9728) so clients can discover the authorization server. Configure `Resource`, `PublicOrigin`, and at least one entry in `AuthorizationServers`. These values are never derived from the request host. `MapCratisArcMcp` maps the metadata route under every profile, so a client probing a personal-access-token-only endpoint gets a JSON `404` rather than a fallback page. Startup validates them:

- `PublicOrigin` must be an origin (no path, query, or fragment) and use `https`. Plain `http` is accepted only for loopback. This applies under every profile whenever `PublicOrigin` is set, including the personal access token profile.
- `Resource` must be `PublicOrigin` plus `Path`, with no fragment, query, or user info.
- Each authorization server must be an absolute `https` URI without user info, a query, or a fragment. Plain `http` is accepted only for loopback.

## Origin check

Browsers attach an `Origin` header to cross-site requests, which lets a web page call a local MCP endpoint. The endpoint therefore refuses a request whose `Origin` is not allowed, with `403`. A request without an `Origin` header, such as one from a command-line or desktop client, is allowed. The allowed set is `PublicOrigin` plus `AllowedOrigins`:

```csharp
mcp.Configure(options =>
{
    options.PublicOrigin = "https://notes.example.test";
    options.AllowedOrigins.Add("https://app.example.test");
});
```

This applies under every profile, including `PatOnly`. If browser-based clients call your endpoint, list their origins, otherwise every call from them is refused with `403 origin_not_allowed`. Each entry must be an `https` origin (loopback may use `http`). Comparison ignores case. More than one `Origin` value, or an `Origin` that is not an origin (such as `null` or a URL with a path), is refused.

## Related

- [What Arc MCP is](index.md)
- [Options reference](reference.md)
- [Capability hosting](../capability-hosting.md)
- [Endpoint groups](../endpoint-groups.md)
- [Verified callers](../asp-net-core/verified-callers.md)
