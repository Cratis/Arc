---
title: Cratis Arc configuration keys
description: Look up every configuration key a C# Arc application reads, with its environment variable form, default and when to change it.
---

Use this page to find the key for a setting, its environment variable and its default. Arc reads configuration from four sections plus one root-level group:

| Section | Read by | Bound with |
| --- | --- | --- |
| `Cratis:Arc` | `AddCratisArc` and `AddCratis` | The `ArcOptions` binder. See [Configuring Arc](index.md#three-ways-to-configure). |
| `Cratis:Chronicle` | `AddCratis` and `WithChronicle()` | The Chronicle client options. Chronicle owns most keys; see the [ChronicleOptions reference](/chronicle/configuration/chronicle-options/). |
| `Cratis:MongoDB` | `WithMongoDB()` | `MongoDBOptions`. |
| `Cratis:EntityFrameworkCore` | `WithEntityFrameworkCore()` | `EntityFrameworkCoreOptions`, property by property. |
| `ReadProtection` (no `Cratis:` prefix) | Read protection at startup and run time | Read straight from `IConfiguration`. |

Environment variables use `__` where the key has `:`. `Cratis:Arc:Tenancy:HttpHeader` becomes `Cratis__Arc__Tenancy__HttpHeader`. List items take an index: `Cratis__Arc__Query__AllowedWebSocketOrigins__0`.

Keys are case-insensitive. A configured value is overridden by the `configureOptions` callback you pass to `AddCratisArc`, because the callback runs after binding.

## Names to prefer

Some settings exist under two names or in two forms. Both work today. Use the name in the second column.

| Setting | Prefer | Also accepted | Notes |
| --- | --- | --- | --- |
| Tenant used by `Fixed` and `Development` resolvers | `Tenancy:FixedTenantId` | `Tenancy:DevelopmentTenantId` | Reading or writing either sets both. Supply one. |
| Trust for unsigned identity headers | `TrustForwardedIdentityHeaders` | `Introspection:TrustForwardedIdentityHeaders` | The `Introspection` key is marked obsolete in its documentation. Setting it turns on the host-wide key. |
| Add Chronicle to Arc | `WithChronicle()` from `Cratis.Arc` (ASP.NET Core host, `ChronicleAspNetCoreOptions`) | `WithChronicle()` from `Cratis.Arc.Chronicle` (generic host, `ChronicleClientOptions`) | Same method name, different options type. Which one you get depends on the package you reference and the namespaces in scope. See [Cratis package](../chronicle/cratis-package.md). |
| Register MongoDB | `WithMongoDB()` | `UseCratisMongoDB()`, `AddCratisMongoDB()` | Register once; do not combine them. |
| React `<Arc>` base path | Leave `apiBasePath` and `origin` as needed | `basePath` | `basePath` does nothing. Generated proxy routes already include the API prefix. |

## `Cratis:Arc`

The first table covers keys already described in [The ArcOptions tree](index.md#the-arcoptions-tree). It lists only key, default and a one-line purpose, so follow the links for detail.

### Core

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `ExposeExceptionDetails` | `true` only when `ASPNETCORE_ENVIRONMENT` is `Development`, otherwise `false` | Include exception messages and stack traces in results. Leave `false` in production. See [Environment detection](#how-arc-detects-development). |
| `CorrelationId:HttpHeader` | `X-Correlation-ID` | Rename the correlation header to match your gateway. |
| `Calendar:TimeZone` | `UTC` | Set an IANA id such as `Europe/Oslo`. An unknown id stops the host. See [Application calendar](../application-calendar.md). |
| `Hosting:ApplicationUrl` | `http://+:5001/` | Arc.Core host only. The default binds every network interface; use `http://localhost:5001/` for local-only. |
| `IdentityDetailsProvider` | not set (discovered) | Code only in practice; set a type when discovery finds none or several. |

### Commands and queries

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `Command:MaximumCallerAllowedSeverity` | `Warning` | Highest severity a caller may allow through the severity header. See [validation severity filtering](../commands/validation-severity-filtering.md). |
| `Command:IgnoreCallerAllowedSeverity` | `false` | Ignore the caller header entirely. |
| `Query:KeepAliveInterval` | `00:00:30` | Idle keep-alive for observable query connections; zero or negative disables it. |
| `Query:AllowedWebSocketOrigins` | not set, which accepts any origin | Set to your front-end origins for production. See [Restricting WebSocket origins](../queries/observable-query-demultiplexer.md#restricting-websocket-origins). |
| `QueryHealth:RequireAuthentication` | unset: anonymous in Development, authenticated elsewhere | Set `true` to require sign-in in Development as well. See [Query health](../queries/query-health.md). |
| `QueryHealth:Roles` | not set | Restrict the query-health feed to these roles. |

`CommandOperationOptions.CompensationTimeout` (default 30 seconds) is not part of `ArcOptions`. It is read from `IOptions<CommandOperationOptions>`, so set it with `services.Configure<CommandOperationOptions>(...)` or bind it yourself; `Cratis:Arc` does not bind it. See [command operations reference](../commands/operations/reference.md).

### Routes

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `GeneratedApis:RoutePrefix` | `api` | Prefix of generated command and query routes. Must match the proxy generator. See [Settings that must match](../proxy-generation/Configuration/routing.md#settings-that-must-match-the-proxy-generator). |
| `GeneratedApis:SegmentsToSkipForRoute` | `0` | Must match `CratisProxiesSegmentsToSkip`. |
| `GeneratedApis:IncludeCommandNameInRoute` | `true` | Inverse of `CratisProxiesSkipCommandNameInRoute`. |
| `GeneratedApis:IncludeQueryNameInRoute` | `true` | Inverse of `CratisProxiesSkipQueryNameInRoute`. |
| `GeneratedApis:EnableQueryHttpMethod` | `true` | Set `false` when infrastructure rejects the HTTP `QUERY` method. |
| `EndpointGroups:Ports`, `EndpointGroups:RoutePrefixes` | empty | Serve groups of endpoints on separate ports or prefixes. See [Endpoint groups](../endpoint-groups.md). |
| `EndpointGroups:ServeDefaultGroupOnGroupPorts` | `false` | Also serve the default group on group ports. |

### Identity, tenancy and discovery

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `TrustForwardedIdentityHeaders` | `false` | Required before `AddCratis()` authenticates anyone. See [AddCratis authenticates nobody](#addcratis-authenticates-nobody-until-you-opt-in). |
| `Introspection:Enabled` | `true` | Set `false` on deployed hosts that need no catalogs. |
| `Introspection:IdentityDiscovery` | `true` | Set `false` on deployed hosts that need no user and tenant lists. |
| `Introspection:RequireAuthentication` | unset: anonymous in Development, authenticated elsewhere | Leave unset unless you must change it. See [Introspection production access](../introspection/index.md#production-access). |
| `Introspection:Roles` | not set | Restrict discovery to these roles. |
| `Tenancy:ResolverType` | `Header` | How the tenant is resolved. |
| `Tenancy:HttpHeader` | `x-cratis-tenant-id` | Tenant header name. |
| `Tenancy:BaseDomain` | empty | Required when `ResolverType` is `Subdomain`. |
| `Tenancy:QueryParameter` | `tenantId` | Query parameter for the `Query` resolver. |
| `Tenancy:ClaimType` | `tenant_id` | Claim for the `Claim` resolver. |
| `Tenancy:FixedTenantId` | `development` | Tenant for `Fixed` and `Development`. Prefer this name. |
| `Tenancy:IngressValidatesTenant` | `false` | Assert that a trusted ingress validates caller-to-tenant membership. It only silences a startup diagnostic. See [Tenancy configuration](../tenancy/configuration.md). |
| `Tenancy:StrictValidatorMembership` | `false` | Recommended `true` for new applications. |
| `ConnectionResolution:*` | resolver `Default`, cache 5 minutes | Per-tenant database connections. The same tree exists under `Cratis:MongoDB`, `Cratis:EntityFrameworkCore` and Neo4j. See [Connection resolution](../tenancy/connection-resolution.md). |
| `OnBehalfOf:*` | `Enabled` is `false` | Signed acting-on-behalf-of assertions. See [Acting on behalf of a user](../asp-net-core/acting-on-behalf-of.md). |
| `ForwardedIdentity:*` | `Enabled` is `false` | Signed forwarded identity. See [Signed forwarded identity](../asp-net-core/signed-forwarded-identity.md). |

### Capabilities

`CapabilitiesOptions` configures [capability hosting](../capability-hosting.md), the verified-caller port that [MCP](../mcp.md) requires. Set it in code; this class has no documented configuration section.

| Property | Default | Purpose and when to change it |
| --- | --- | --- |
| `EndpointGroup` | `capabilities` | The endpoint group whose ports require a verified caller. |
| `GatedEndpoints` | not set: every request on that port is gated | Narrow which endpoints on the capability port are gated. An explicitly empty selector is invalid. |
| `AllowedPrincipalKinds` | `Person` and `DelegatedActor` | Change which kinds of caller are admitted. |

### MCP

Set these in the `WithMcp(...)` callback with `McpBuilder.Configure`. They are properties of `McpOptions`; see [Serving MCP tools](../mcp.md) for the startup requirements.

| Property | Default | Purpose and when to change it |
| --- | --- | --- |
| `Path` | `/mcp` | Route of the MCP endpoint. |
| `Resource`, `PublicOrigin`, `AuthorizationServers`, `ScopesSupported`, `AllowedOrigins` | not set | Public identity and allowed browser origins. |
| `ApplicationName` | not set | Used to derive default scopes. |
| `ReadScope`, `WriteScope` | `<ApplicationName>:read`, `<ApplicationName>:write`; null when no application name is set | Scopes that read and write tools require. |
| `WriteScopes` | empty | Further write scopes. |
| `Profile` | `PatOnly` | Accepted credential profile. |
| `MaxRequestBodySize` | 1,048,576 bytes (1 MiB) | Largest MCP request body read. Larger requests are refused before any tool runs. Raise it only if a tool takes large arguments. |
| `ExclusiveTools` | `true` | Serve only the tools of the configured ceiling. Setting `false` serves other registered tools with no scope check and logs a startup warning. |

## `Cratis:Chronicle`

Arc adds one setting of its own: `WithChronicle()` on ASP.NET Core sets `EventStoreNamespaceResolverType` to `TenantNamespaceResolver`, so the tenant selects the Chronicle namespace. Your `configureChronicleOptions` callback runs after it and can replace that.

Everything else, including the event store name and the connection string, is a Chronicle option. See the [ChronicleOptions reference](/chronicle/configuration/chronicle-options/).

`ProtectedReadModelKeyOptions.Strict` (default `false`) has no configuration key. Set it with `services.Configure<ProtectedReadModelKeyOptions>(...)`. See [protected key accessors](../chronicle/protected-key-accessors.md).

## `Cratis:MongoDB`

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `Server` | none, required | MongoDB connection string. Keep credentials in secret configuration. |
| `Database` | none, required | Database name. |
| `DirectConnection` | not set | When unset the connection string's setting is kept. |
| `EnableResilience` | `true` | Treat "collection not found" as an empty result on async calls. Keep it on for Cosmos DB and change streams opened before a collection exists. |
| `LogCommandBodies` | `false` | Log MongoDB command bodies and driver failure messages while diagnosing. They can contain personal data; never enable in production. |
| `ConnectionResolution:*` | resolver `Default` | Per-tenant connections. |
| `Sinks:<name>:*` | none | Named sinks. See [named sinks](../mongodb/named-sinks.md). |

Missing `Server` or `Database` fails host startup. See [Required settings](../mongodb/getting-started.md#required-settings-and-the-error-when-they-are-missing).

## `Cratis:EntityFrameworkCore`

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `ConnectionString` | empty | Connection string. While empty, Arc does not auto-discover `DbContext` types. |
| `AutoDiscoverDbContexts` | `true` | Register discovered `DbContext` types. |
| `ConnectionResolution:*` | resolver `Default` | Per-tenant connections. |

`JsonConverters` is code-only. Other `EntityFrameworkCoreOptions` properties are not read from configuration.

## Root-level `ReadProtection`

These keys are read at the configuration root, not under `Cratis:Arc`. `Cratis:Arc:ReadProtection:MaximumMode` has no effect. The environment form is `ReadProtection__MaximumMode`.

| Key | Default | Purpose |
| --- | --- | --- |
| `ReadProtection:MaximumMode` | `On` | A ceiling that can only lower the mode. See [Cap the mode at run time](../chronicle/consistency/adopting.md#cap-the-mode-at-run-time). |
| `ReadProtection:StartupAdmissionCheck` | `Default` | `Default`, `Log` or `Off`. |
| `ReadProtection:RequireGeneratedMetadata` | `false` (always required where dynamic code is unavailable) | Fail instead of falling back to reflection for read-model key lookup. |
| `ReadProtection:MeasureSampleRate` | `0.1` | Fraction of commands scanned by Measure, 0 to 1. |
| `ReadProtection:MeasureTailTimeout` | `250` | Milliseconds, 1 to 5000. |
| `ReadProtection:MeasureScanTimeout` | `5000` | Milliseconds, 1 to 5000. |
| `ReadProtection:MeasureScanLimit` | `500` | Events per scan, 1 to 500. |

The mode itself is a build setting, `<CratisReadProtection>`, not a configuration key. See [Read protection](../chronicle/consistency/index.md).

## Environment variables Arc reads

| Variable | Effect |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` turns on the `ExposeExceptionDetails` default. Other development checks use the host environment when one is registered. See [Environment detection](#how-arc-detects-development). |

## How Arc detects Development

`ArcOptions.ExposeExceptionDetails` defaults from `ASPNETCORE_ENVIRONMENT` alone, read once when the process starts. A host started with `DOTNET_ENVIRONMENT=Development` or with the environment set in code has `ExposeExceptionDetails` off. Set it in configuration or code if you want it on:

```json
{ "Cratis": { "Arc": { "ExposeExceptionDetails": true } } }
```

Discovery access and query-health access check the host environment first (`IHostEnvironment.IsDevelopment()`), including `DOTNET_ENVIRONMENT`, and fall back to `ASPNETCORE_ENVIRONMENT` only when no host environment is registered. Arc's service-provider scope validation also follows the host environment. Do not switch a deployed host to Development to restore access.

## `AddCratis()` authenticates nobody until you opt in

`AddCratis()` registers the Microsoft Identity Platform header adapter. With `TrustForwardedIdentityHeaders` at its default of `false`, that adapter ignores the `x-ms-client-principal*` headers, so every request is anonymous and endpoints that require a user return 401. The host logs a warning on every startup naming the setting.

Set `Cratis:Arc:TrustForwardedIdentityHeaders` to `true` only when every request comes through an ingress that authenticates callers and replaces those headers. Otherwise register an authentication scheme that validates callers. See [Microsoft Identity](../asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).
