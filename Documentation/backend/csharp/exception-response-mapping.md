---
title: Exception response mapping
description: "Map application-owned exceptions to safe HTTP statuses, stable public error codes, and Retry-After without exposing exception details."
---

Use `IExceptionResponseMapper` when an application failure needs a public meaning beyond HTTP 500. For example, a closing namespace can return 503 with a delay, while a namespace requiring operator recovery can return 409 without encouraging a retry. Arc supplies the mapping seam; the application or adapter owns its exception types and recovery policy.

## Define a mapper

Implement `Cratis.Arc.Exceptions.IExceptionResponseMapper` in an assembly discovered by your Arc application. Arc discovers implementations by convention and resolves them from the request or subscription scope; no separate mapper registration call is required. Constructor injection is supported.

This example uses application-owned exception types. It does not depend on Chronicle or change Chronicle lifecycle behavior.

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Cratis.Arc.Exceptions;

public sealed class NamespaceClosing : Exception;
public sealed class NamespaceRecoveryRequired : Exception;

public sealed class NamespaceExceptionResponses : IExceptionResponseMapper
{
    public bool TryMap(Exception exception, [NotNullWhen(true)] out ExceptionResponse? response)
    {
        response = exception switch
        {
            NamespaceClosing => new ExceptionResponse(
                HttpStatusCode.ServiceUnavailable,
                "namespace_closing",
                new Dictionary<string, string> { ["Retry-After"] = "5" }),
            NamespaceRecoveryRequired => new ExceptionResponse(
                HttpStatusCode.Conflict,
                "namespace_recovery_required"),
            _ => null
        };
        return response is not null;
    }
}
```

The code identifies a failure category, not an exception message. Choose constant public codes; never derive them from exception text, stack traces, tenant identifiers, credentials, or connection details. Clients can translate these codes into appropriate feedback.

`ExceptionResponse` defensively copies its headers. Responses accept only failure statuses (400–599), a 1–128 character ASCII code containing letters, digits, `.`, `_`, or `-`, and the `Retry-After` header. Its value must be nonnegative integer seconds or an RFC 1123 HTTP date. CR/LF and other headers are rejected at the boundary. Do not use the response for cookies, redirects, authentication challenges, or arbitrary headers.

## Precedence and safe failure

The existing `ICommandExceptionMapper` still runs first: a command-domain validation mapping is not reinterpreted as an HTTP exception mapping. Authorization failures, validation failures, built-in retryable failures, not-ready query results, and Arc's invalid authorization configuration failures retain their existing behavior. Only an otherwise ordinary exception is eligible.

Mappers run until one accepts the exception; there is no priority or ordering API. Make their matching types disjoint. A declined or absent mapper leaves the existing result, status, and redaction policy unchanged. A throwing mapper, resolution failure, or invalid accepted response is logged and falls back to the existing failure behavior. Invalid metadata is never written to the response.

An accepted mapping adds optional `errorCode` to the command or query result. It does **not** add a new validation result, make the operation successful, or set `isRetryable`. Mapped exceptions always use the generic redacted exception message and an empty stack trace, even with `ExposeExceptionDetails` enabled. Unmatched exceptions continue to follow that option. Exception instances and mapper response objects are not serialized.

## Transport behavior

| Transport | Accepted mapping |
|---|---|
| Native HTTP and ASP.NET Core command/query responses | Failure status, optional `errorCode`, and allowed `Retry-After` before the response starts |
| Observable HTTP snapshot | Same status and header behavior before committing the snapshot |
| SSE, WebSocket, and multiplexed query emissions | Safe failure envelope with optional `errorCode`; cannot change an already-started HTTP status or headers |
| MCP command/query tools | Existing tool error semantics with `outcome.errorCode` in text and structured content; no new HTTP status or header semantics |

The TypeScript command and query results expose `errorCode?: string`. It is absent for older servers and unmapped failures. State conversion and query caching preserve it; a later result without a code clears it. Check the code only after checking the result's outcome, rather than inferring success from a missing code.

A 503 and `Retry-After` describe temporary unavailability; they do not enable automatic retries in Arc clients. A 409 such as `namespace_recovery_required` can require operator intervention instead. The application owns whether retrying is safe, especially for commands with side effects. Preserve the correlation ID for investigation and do not parse exception messages as a protocol.
