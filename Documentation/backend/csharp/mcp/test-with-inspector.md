---
title: "Test an Arc MCP endpoint with MCP Inspector"
description: Use MCP Inspector to list and call the tools of a Cratis Arc MCP endpoint, and set the Origin allowlist a browser-based Inspector needs.
---

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) is a visual client for browsing a server's tools and calling them. Use it to check descriptions, schemas and scope filtering before you connect an agent.

## Connect

1. Start your application and issue a PAT with the scopes you want to test.
2. Start the Inspector: `npx @modelcontextprotocol/inspector`.
3. Choose the **Streamable HTTP** transport.
4. Enter the URL, `https://notes.example.test:<capabilityPort>/mcp`.
5. Add the header `Authorization` with the value `Bearer <token>`.
6. Connect, open the **Tools** tab and list the tools.

Try a token with only the read scope and confirm the command tool is missing from the list. Call a tool and read the `outcome` in the result.

## If the Inspector is refused with 403

Arc checks the `Origin` header on every request. A request with no `Origin` header is allowed, which is the case for command-line clients. A browser sends one, and Arc refuses any origin that is not in the allowlist with HTTP 403 `{"error":"origin_not_allowed"}`.

When the Inspector page or its proxy sends a browser origin, add it to `AllowedOrigins` for the test run:

```csharp
mcp.Configure(options =>
{
    options.AllowedOrigins.Add("http://localhost:6274");
});
```

Rules for the allowlist:

- Each entry is a bare origin: scheme, host and optional port, with no path, query, fragment or user info.
- It must be `https`, or `http` for a loopback host only.
- Comparison ignores case. `PublicOrigin` is always allowed.
- A request with several `Origin` headers, or the origin `null`, is always refused.

Set the `Cratis.Arc.Mcp` log category to Debug: `McpAdmission` then logs each refused origin and the reason, so you can copy the exact origin. You can also check it in the browser developer tools. Do not rely on the port above, and remove the entry when you finish. Keep production `AllowedOrigins` empty unless a real browser client needs it.

If the Inspector connects through its own proxy process, the proxy makes the request and may send no `Origin` header; in that case no allowlist entry is needed.

## Local TLS

The capabilities port is a separate listener. If you test against a local development certificate, make sure the Inspector's process trusts it, or test against an `http` loopback URL.
