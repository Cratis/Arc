---
title: "Connect Claude, VS Code and other clients to Arc MCP"
description: Connect command-line and IDE MCP clients to a Cratis Arc MCP endpoint with a personal access token, and learn what hosted connectors need instead.
---

Arc MCP uses the MCP streamable HTTP transport. Clients POST JSON-RPC to the endpoint (default `/mcp` on the capabilities port) and authenticate with `Authorization: Bearer <token>`.

## Command-line and IDE clients: use a PAT

The default `PatOnly` profile accepts personal access tokens only. Issue a token with the scopes the client needs (see [Quick start](quick-start.md)), then configure the client with the endpoint URL and the header. The examples use `https://notes.example.test:<capabilityPort>/mcp`.

### Claude Code

```bash
claude mcp add --transport http notes https://notes.example.test:<capabilityPort>/mcp \
  --header "Authorization: Bearer $PAT"
```

### Claude Desktop

Claude Desktop can connect to a remote server through the `mcp-remote` bridge:

```json
{
  "mcpServers": {
    "notes": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "https://notes.example.test:<capabilityPort>/mcp",
        "--header",
        "Authorization: Bearer ${PAT}"
      ],
      "env": { "PAT": "<your token>" }
    }
  }
}
```

### Visual Studio Code

In `.vscode/mcp.json`:

```json
{
  "inputs": [
    { "type": "promptString", "id": "notes-pat", "description": "Notes token", "password": true }
  ],
  "servers": {
    "notes": {
      "type": "http",
      "url": "https://notes.example.test:<capabilityPort>/mcp",
      "headers": { "Authorization": "Bearer ${input:notes-pat}" }
    }
  }
}
```

Do not commit a token. Prompt for it, or read it from the environment.

## What the server returns to a client without a token

A request with no accepted token gets HTTP 401 `{"error":"unauthorized"}` and a `WWW-Authenticate` header. Under `PatOnly` it is `Bearer realm="<ApplicationName>"` (or `Bearer` with no application name). A client that shows this has reached the server but has no valid PAT.

## Hosted connectors: OAuth

Hosted connectors, where a service such as an AI chat product calls your server for a user, cannot be given a PAT by hand. They need an OAuth flow, which Arc does not provide. Arc verifies OAuth grants through your installation and publishes protected resource metadata; your own authorization server issues the tokens.

Choose the `OAuthOnly` profile, or `Combined` to keep PATs working too, and set:

```csharp
mcp.Configure(options =>
{
    options.ApplicationName = "notes";
    options.Profile = McpProfile.Combined;
    options.PublicOrigin = "https://notes.example.test";
    options.Resource = "https://notes.example.test/mcp";
    options.AuthorizationServers.Add("https://login.example.test");
});
```

For these profiles:

- `PublicOrigin` is required.
- `Resource` must equal `PublicOrigin` plus `Path`.
- `AuthorizationServers` needs at least one entry. Each must be an `https` URL (plain `http` is allowed for loopback only), with no query, fragment or user info.
- Metadata is served at `/.well-known/oauth-protected-resource` plus the path (`/.well-known/oauth-protected-resource/mcp`). It lists `resource`, `authorization_servers` and `scopes_supported`.
- An unauthenticated request gets `WWW-Authenticate: Bearer resource_metadata="<PublicOrigin>/.well-known/oauth-protected-resource<Path>"`, which points the connector at the metadata.

Under `PatOnly` the metadata URL returns 404 `{"error":"not_found"}`.

Whether a particular hosted connector can register with your authorization server depends on that server and on the connector. This is outside Arc.
