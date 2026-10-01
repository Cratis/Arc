---
title: Migrating to secure defaults
description: Arc no longer trusts forwarded identity headers by default. Who is affected, how to tell, and the one setting that restores the previous behavior.
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
