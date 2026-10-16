---
title: Command input binding
description: Understand malformed command input, authorization precedence, and explicit-offset date-time binding.
---

A malformed date or a string sent for a number is not a failed business rule: Arc could not construct the command. Model-bound execute and `/validate` endpoints report this as a validation result with `reason: "malformedRequest"`, after the authorization checks that can run without a command instance.

## Authorization before reporting input errors

Arc reads the body first, but does not report a binding failure until it checks the command's effective authentication, roles, scheme selection, anonymous/guest handling, and fallback requirements. A denied caller gets **403 Forbidden**, not an input error. Host configuration failures remain failures rather than becoming validation results.

For named ASP.NET Core policies, Arc evaluates the policy with a **null resource** only when every requirement in every resolved policy is resource-free:

- `DenyAnonymousAuthorizationRequirement`, `RolesAuthorizationRequirement`, `ClaimsAuthorizationRequirement`, and `NameAuthorizationRequirement` are resource-free.
- Custom requirements opt in by implementing `Cratis.Arc.Authorization.IResourceFreeRequirement` from `Cratis.Arc`.
- `AssertionRequirement` is not resource-free. An assertion can read the resource even when a particular callback happens not to.

Implement the marker only when all handlers for that requirement read the user, never the resource, directly or indirectly. A claim-to-command-property comparison must **not** implement it. The marker is a promise made by your application; Arc cannot inspect handler code to prove it.

An application-registered extra handler for a built-in requirement (for example `RolesAuthorizationRequirement`) sees a `null` resource on this path; if it requires a resource, at worst that caller gets 400 instead of 403.

If any resolved policy has a resource-dependent requirement, Arc does not evaluate the policy set for an unbindable body. A caller meeting authentication and role requirements then gets **400 Validation**, even if a policy would deny a successfully bound command. Native Core `IAuthorizationPolicy` implementations also remain resource-dependent: they have no ASP.NET requirement set to classify. `IAuthorizationCommandFilter` implementations never run without a command instance.

No command context values, execution scopes, `Provide()`, or `Handle()` run on this path. Successfully bound commands retain the full authorization pipeline, including resource policies and authorization command filters.

## Members and messages

The validation result's `members` identifies the failing JSON path when the deserializer supplies one:

| Deserializer path | Validation member |
| --- | --- |
| `$.from` | `from` |
| `$.Items[2].When` | `items[2].when` |
| `$` or no path | No member |

Arc camel-cases each dotted segment and retains array indices. Root failures and concept converters that throw a non-JSON parsing exception may have no member. Custom JSON property names also pass through this casing normalization.

Ordinary binding failures use **“The request body could not be read or is not valid for this command.”** Arc does not echo arbitrary converter messages, parser details, or target type names. Arc-owned explicit-offset refusals use **“DateTime command property 'from' requires an explicit offset or Z.”**, substituting the normalized member.

Array paths identify the precise failing value, but a frontend field named only `items` does not automatically match `items[2].when`.

## Require explicit date-time offsets

Local date-time text is ambiguous when a rule compares instants. Opt in on the properties whose wire values must identify an instant. This DTO declaration works in an existing model-bound command or as a controller body; the marker does not itself expose an endpoint:

```csharp
using System;
using Cratis.Arc.Commands;

public record TimeWindow(
    [property: ExplicitOffset] DateTimeOffset From,
    [property: ExplicitOffset] DateTimeOffset To);
```

Use the `property:` target on positional records. The attribute accepts properties and fields, not method or constructor parameters. It supports `DateTimeOffset`, `DateTime`, and their nullable forms; an unsupported type raises `ExplicitOffsetRequiresDateTimeOrDateTimeOffset` when JSON metadata is resolved.

Accepted strings use an uppercase `T`, hours and minutes, optional seconds with up to seven fractional digits, and either an offset such as `+02:00` or uppercase `Z`:

- `2026-10-16T12:30+02:00`
- `2026-10-16T10:30Z`
- `2026-10-16T12:30:00.1234567+02:00`

Date-only text, missing offsets, lowercase `t`/`z`, and more than seven fractional digits are refused. Wrong token types and null for a non-nullable property produce generic malformed-request results. Nullable properties accept JSON null.

`DateTimeOffset` keeps its supplied offset and tick precision; its comparisons already compare instants. `DateTime` becomes UTC with `Kind == Utc`. Writing uses the normal JSON date-time string representation. This does not add date-ordering rules: use server validation for the comparison. Generated clients serialize JavaScript `Date` values with `toISOString()` (including `Z`), but JavaScript retains only milliseconds; [date ordering stays server-side](../proxy-generation/validation.md#fluentvalidation-rule-reference), with no client clock rule.

The marker applies to JSON-bound properties, including nested collection elements. It does not change query-string or route-scalar binding, and unmarked properties keep their existing wire behavior. System.Text.Json source-generated metadata does not support derived `JsonConverterAttribute` attributes; use reflection-based metadata for these properties rather than assuming an application-supplied JSON context honors the marker.

## Controller differences

ASP.NET Core authorization already runs before MVC binding. MVC controls formatter messages and response short-circuiting. Arc normalizes formatter ModelState keys such as `$.from` into member `from`; the explicit-offset formatter message remains **“The value requires an explicit offset or Z.”** rather than the model-bound member-specific message.

An `[ApiController]` automatic ProblemDetails response can short-circuit before Arc's action filter, so it is not guaranteed to use Arc's command-result envelope. See [controller-based commands](./controller-based.md) and [pre-flight validation](./command-validation.md) for those separate execution paths.
