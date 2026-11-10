---
title: "Arc MCP audit log: who called which tool"
description: Read the Cratis Arc MCP audit log to see which agent called which tool, for whom and with what outcome. It holds identifiers only, never token values.
---

Every MCP `tools/call` writes one Information log entry. Use it to answer "which agent did what, on whose behalf".

| Property | Value |
|---|---|
| Category | `Cratis.Arc.Mcp.McpCallAudit` |
| Event id | 5102 |
| Level | Information |

An entry reads like this:

```text
MCP tool 'add_note' (Command) called by person '<person id>' in tenant '<tenant>' with credential '<credential id>' (PersonalAccessToken): succeeded (correlation <correlation id>)
```

## What an entry holds

| Field | Meaning |
|---|---|
| Tool | The tool name you declared. |
| Kind | `Query`, `Command` or `Custom` (a curated tool). |
| Person and tenant | Identifiers of the verified caller. |
| Credential id and kind | The identifier of the credential, and its kind such as `PersonalAccessToken`. Use it to find and revoke the token. |
| Outcome | See below. |
| Correlation id | Joins the entry to the command or query it ran and to other logs of the request. |

An entry never contains a token value, the call arguments or the result. Credentials are identified by id only.

## Outcomes

| Outcome | Meaning |
|---|---|
| `succeeded` | The operation ran and succeeded. |
| `failed` | The operation ran and failed, including validation failures. |
| `insufficient_scope` | The caller lacked a required scope. |
| `not_allowed` | An authorizer or the operation's authorization refused. |
| `invalid_arguments` | The arguments did not match the tool's input schema. |
| `malformed` | The request was not a valid tool call. |
| `error` | The operation threw. |

Scope and authorizer refusals are audited, including those made at HTTP admission. A request with no accepted caller has no identity to record and is not audited; it is refused with 401.

## Operating it

- Person and tenant are personal data. You control retention and access for wherever your log provider stores these entries.
- To keep the entries, make sure the `Cratis.Arc.Mcp.McpCallAudit` category is not filtered below Information in `Logging:LogLevel`.
- Refused origins are a separate Debug entry from `McpAdmission` (event 5101), because those requests are refused before any caller is known. See [Troubleshooting](troubleshooting.md).

## Related

- [Authorization](authorization.md)
- [Options reference](reference.md#logging)
