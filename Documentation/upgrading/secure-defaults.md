---
title: Migrating to secure defaults
description: Arc no longer trusts forwarded identity headers or exposes discovery endpoints anonymously outside Development by default. Who is affected, how to tell, and the one setting that restores the previous behavior.
---

Some Arc defaults used to be convenient but unsafe on a deployed host. Arc now ships safe
defaults instead. Read each section below and check whether your application relies on the
old behavior. Each section ends with the single change that restores it.

## Forwarded identity headers need an explicit opt-in

**Who is affected:** applications that authenticate users through the unsigned
`x-ms-client-principal`, `x-ms-client-principal-id` and `x-ms-client-principal-name` headers.
That is every application that calls `builder.AddCratis()`, or
`builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication()`, and runs behind Azure
App Service or Container Apps authentication (EasyAuth) or Cratis AuthProxy. On the Arc.Core
`HttpListener` host it is every application that relies on the built-in
`MicrosoftIdentityPlatformAuthenticationHandler`.

**What changed:** Arc used to trust these headers from any caller. Anyone who could reach the
application directly could send them and act as any user. Arc now ignores them until the host
opts in. Without the opt-in, every request is anonymous and endpoints that require an
authenticated user return 401.

**How to tell:** an ASP.NET Core host that registers the Microsoft Identity Platform scheme
without the opt-in logs a warning that names the setting on every startup. Any host logs a
warning on the first request that carries the headers while they are not trusted.

**What to do:** if every request reaches your application through an ingress that strips
caller-supplied identity headers and sets its own, opt in. In code, add one line to the options
you pass to `AddCratis` or `AddCratisArc`:

```csharp
builder.AddCratis(options => options.TrustForwardedIdentityHeaders = true);
```

Or in configuration, for example as the environment variable
`Cratis__Arc__TrustForwardedIdentityHeaders=true`:

```json
{
  "Cratis": {
    "Arc": {
      "TrustForwardedIdentityHeaders": true
    }
  }
}
```

If the application can be reached without going through such an ingress, do not opt in:
configure a real authentication scheme instead.

`Cratis:Arc:Introspection:TrustForwardedIdentityHeaders` is now obsolete. Setting it still
turns on the host-wide `TrustForwardedIdentityHeaders`, so a host that already set it keeps
working. Move the setting to `Cratis:Arc:TrustForwardedIdentityHeaders`. Startup no longer
refuses protected catalogs behind the header scheme, because the scheme authenticates
nobody until the host opts in.

### Example: Cratis Studio

Cratis Studio runs every service behind AuthProxy and calls `AddCratis` from
`AddStudioCratisService` in
`Source/Infrastructure/CratisServiceConfigurationExtensions.cs` in the Studio repository.
It opts in with one line in the options callback of that `builder.AddCratis(` call (line 49),
next to `options.UseStudioTenancy();` (line 55):

```csharp
options.TrustForwardedIdentityHeaders = true;
```

See [Microsoft Identity](../backend/csharp/asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).

## Discovery endpoints require authentication outside Development

**Who is affected:** applications or tools that read the discovery endpoints of a host running
outside Development without signing in. The discovery endpoints are the command and query
catalogs (`/.cratis/commands` and `/.cratis/queries`) and identity discovery
(`/.cratis/users`, `/.cratis/tenants` and `/.cratis/identity-details/schema`). Command and query
invocation and `/.cratis/me` are not affected.

**What changed:** these endpoints used to be anonymous in every environment. They are now
anonymous only in Development, where Lens and the Cratis CLI read them locally. Everywhere else
they require an authenticated caller, so an anonymous request gets 401. A host outside
Development that has no way to authenticate callers, such as an ASP.NET Core host without a
default authentication scheme or an Arc.Core host without authentication handlers, does not map
them at all (404) and logs a warning that names the setting below on startup. Development is
decided by the host's `IHostEnvironment.IsDevelopment()`, including environment names configured
through `DOTNET_ENVIRONMENT`, command-line arguments or host options. Only when no host
environment is registered does Arc fall back to `ASPNETCORE_ENVIRONMENT`.

**What to do:** nothing, if only signed-in users or local tooling read these endpoints. To
expose them anonymously as before, add one line to the options you pass to `AddCratis` or
`AddCratisArc`:

```csharp
builder.AddCratisArc(options => options.Introspection.RequireAuthentication = false);
```

Or set `Cratis__Arc__Introspection__RequireAuthentication=false`. Outside Development the host
then logs a warning on startup that the endpoints are anonymous.

`Introspection.RequireAuthentication` remains a `bool` for source compatibility. Its getter
returns `false` when unset, but leaving it unset selects the environment default; explicitly
assigning `false` opts into anonymous discovery in every environment.
`Introspection.Roles` now implies authentication on its own instead of needing
`RequireAuthentication: true`.

See [Introspection](../backend/csharp/introspection/index.md#production-access).
