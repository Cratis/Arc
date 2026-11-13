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
| `Cratis:Arc:ReadProtection` and root `ReadProtection` | Read protection at startup and run time | Both sections are accepted; the namespaced key wins when both are set. |

Environment variables use `__` where the key has `:`. `Cratis:Arc:Tenancy:HttpHeader` becomes `Cratis__Arc__Tenancy__HttpHeader`. List items take an index: `Cratis__Arc__Query__AllowedWebSocketOrigins__0`.

Keys are case-insensitive. A configured value is overridden by the `configureOptions` callback you pass to `AddCratisArc`, because the callback runs after binding.

## Names to prefer

Some settings exist under two names or in two forms. Both work today. Use the name in the second column.

| Setting | Prefer | Also accepted | Notes |
| --- | --- | --- | --- |
| Tenant used by `Fixed` and `Development` resolvers | `Tenancy:FixedTenantId` | `Tenancy:DevelopmentTenantId` | Reading or writing either sets both. Supply one. |
| Trust for unsigned identity headers | `TrustForwardedIdentityHeaders` | `Introspection:TrustForwardedIdentityHeaders` | The `Introspection` property is marked `[Obsolete]`. Setting it turns on the host-wide key. |
| Add Chronicle to Arc | `WithChronicle()` from `Cratis.Arc` (ASP.NET Core host, `ChronicleAspNetCoreOptions`) | `WithChronicle()` from `Cratis.Arc.Chronicle` (generic host, `ChronicleClientOptions`) | Same method name, different options type. Which one you get depends on the package you reference and the namespaces in scope. See [Cratis package](../chronicle/cratis-package.md). |
| Register MongoDB | `WithMongoDB()` | `UseCratisMongoDB()`, `AddCratisMongoDB()` | Register once; do not combine them. |
| React `<Arc>` base path | Leave `apiBasePath` and `origin` as needed | `basePath` | `basePath` does nothing. Generated proxy routes already include the API prefix. |

## `Cratis:Arc`

The first table covers keys already described in [The ArcOptions tree](index.md#the-arcoptions-tree). It lists only key, default and a one-line purpose, so follow the links for detail.

### Core

| Key | Default | Purpose and when to change it |
| --- | --- | --- |
| `ExposeExceptionDetails` | `true` only in Development, otherwise `false` | Include exception messages and stack traces in results. Leave `false` in production. See [Environment detection](#how-arc-detects-development). |
| `CorrelationId:HttpHeader` | `X-Correlation-ID` | Rename the correlation header to match your gateway. |
| `Calendar:TimeZone` | `UTC` | Set an IANA id such as `Europe/Oslo`. An unknown id stops the host. See [Application calendar](../application-calendar.md). |
| `Hosting:ApplicationUrl` | `http://+:5001/` | Arc.Core host only. The default binds every network interface; use `http://localhost:5001/` for local-only. |
| `IdentityDetailsProvider` | not set (discovered) | Code only in practice; set a type when discovery finds none or several. |
| `JsonSerializerOptions` | Arc serialization defaults | **Code only**: customize the serializer used by the host. |
| `Artifacts:IncludedAssemblies` | empty collection | **Code only**: call `Artifacts.Include(assembly)` to opt into an assembly's commands and queries. |

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
| `OnBehalfOf:Enabled` | `false` | Accept signed acting-on-behalf-of assertions. |
| `OnBehalfOf:Issuer`, `OnBehalfOf:Audience`, `OnBehalfOf:Service` | empty strings | Expected issuer, audience, and optional service. |
| `OnBehalfOf:SigningKeys` | empty list | Each key has `KeyId` and `PublicKeyPem`, both empty by default. |
| `OnBehalfOf:ClockSkew` | `00:00:30` | Allowed assertion clock skew. |
| `ForwardedIdentity:Enabled` | `false` | Require signed forwarded identity. |
| `ForwardedIdentity:Audience` | empty string | Integration audience. |
| `ForwardedIdentity:Keys` | empty list | Each key has `KeyId` and `SecretBase64`, both empty by default. Provision secrets through a secret provider. |
| `ForwardedIdentity:ClientCertificate:Required` | `false` | Require an admitted immediate forwarding peer. |
| `ForwardedIdentity:ClientCertificate:TrustedCertificateAuthoritiesPath` | not set | PEM authority bundle path. |
| `ForwardedIdentity:ClientCertificate:AllowedDnsNames` | empty list | Admitted peer DNS SAN names. |
| `ForwardedIdentity:ClientCertificate:ReloadInterval` | `00:01:00` | Authority bundle reload interval. |

Both forwarding modes also require `TrustForwardedIdentityHeaders`. See [acting on behalf of a user](../asp-net-core/acting-on-behalf-of.md) and [signed forwarded identity](../asp-net-core/signed-forwarded-identity.md) for validation requirements.

### Connection resolution

These keys are relative to `Cratis:Arc:ConnectionResolution`. The same tree exists under `Cratis:MongoDB`, `Cratis:EntityFrameworkCore` and Neo4j. `Connection` and `Identity` have the same resolver shape; replace `<slot>` below with either name. See [connection resolution](../tenancy/connection-resolution.md).

| Key | Default | Purpose |
| --- | --- | --- |
| `CacheDuration` | `00:05:00` | In-memory resolved-connection lifetime. |
| `<slot>:Resolver` | `Default` | Configured connection, template, HTTP, or custom resolver. |
| `<slot>:Template:ConnectionString`, `<slot>:Template:Database`, `<slot>:Template:Schema` | not set | Optional templates; unset parts retain configured values. |
| `<slot>:Http:Endpoint` | not set | Resolution endpoint. |
| `<slot>:Http:Headers` | empty dictionary | Additional request headers. |
| `<slot>:Http:Timeout` | `00:00:10` | Resolution request timeout. |
| `<slot>:Http:Authentication:Type` | `None` | Bearer-token acquisition method. |
| `<slot>:Http:Authentication:Scope` | not set | Requested scope. |
| `<slot>:Http:Authentication:TokenEndpoint` | not set | OAuth token endpoint. |
| `<slot>:Http:Authentication:ClientId`, `<slot>:Http:Authentication:ClientSecret` | not set | Client credentials; supply secrets through a secret provider. |
| `<slot>:Http:Authentication:Credential` | `ManagedIdentity` | Azure credential type. |
| `<slot>:Http:Authentication:RefreshMargin` | `00:05:00` | Token renewal margin. |
| `<slot>:CustomType` | not set | Resolver type; **code only**. |

`IsConfigured` is computed, not a configuration key.

### Capabilities

`CapabilitiesOptions` configures [capability hosting](../capability-hosting.md), the verified-caller port that [MCP](../mcp/index.md) requires. Set it in code; this class has no documented configuration section.

| Property | Default | Purpose and when to change it |
| --- | --- | --- |
| `EndpointGroup` | `capabilities` | The endpoint group whose ports require a verified caller. |
| `GatedEndpoints` | not set: every request on that port is gated | **Code only**: use `CapabilityEndpointSelector.ForRoutePattern` or `ForEndpointMetadata<T>()`. An explicitly empty selector is invalid. |
| `AllowedPrincipalKinds` | `Person` and `DelegatedActor` | Change which kinds of caller are admitted. |
| `GateAfterRouting` | `false` | Gate after endpoint routing; a selector or protocol integration can enable this. |

### MCP

Set these in the `WithMcp(...)` callback with `McpBuilder.Configure`. They are properties of `McpOptions`; see [Serving MCP tools](../mcp/index.md) for the startup requirements.

| Property | Default | Purpose and when to change it |
| --- | --- | --- |
| `Path` | `/mcp` | Route of the MCP endpoint. |
| `Resource`, `PublicOrigin` | not set | Canonical public resource URI and origin; required for OAuth and combined profiles. |
| `AuthorizationServers`, `ScopesSupported`, `AllowedOrigins` | empty lists | Authorization servers, supported scopes, and additional browser origins. |
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

## Read protection

The runtime, measurement, and startup-admission keys below accept both `Cratis:Arc:ReadProtection:<key>` and the original root `ReadProtection:<key>`. The namespaced key wins when both are set. For example, use `Cratis__Arc__ReadProtection__MaximumMode` as an environment variable. `ArcOptions.ReadProtection` mirrors the configuration; assigning its properties in an options callback does not change runtime policy.

| Key (relative to either section) | Default | Purpose |
| --- | --- | --- |
| `DefaultMode` | `On` | Mode when a command has no explicit declaration; `MaximumMode` can cap it. |
| `MaximumMode` | `On` (no cap) | A ceiling that can only lower the mode. See [Cap the mode at run time](../chronicle/consistency/adopting.md#cap-the-mode-at-run-time). |
| `RequireGeneratedMetadata` | `true` (always required where dynamic code is unavailable) | Fail instead of falling back to reflection for read-model key lookup. Set `false` only to opt out explicitly. |
| `EffectivenessWarnings` | `false` | Warn when enabling protection without an Arc command pipeline. A registered warning sink implies `true` unless configured explicitly. |

Chronicle's measurement and startup-admission settings also accept both sections. The table uses the original root spelling:

| Key | Default | Purpose |
| --- | --- | --- |
| `ReadProtection:StartupAdmissionCheck` | `Default` | `Default`, `Log` or `Off`. |
| `ReadProtection:MeasureSampleRate` | `0.1` | Fraction of commands scanned by Measure, 0 to 1. |
| `ReadProtection:MeasureTailTimeout` | `250` | Milliseconds, 1 to 5000. |
| `ReadProtection:MeasureScanTimeout` | `5000` | Milliseconds, 1 to 5000. |
| `ReadProtection:MeasureScanLimit` | `500` | Events per scan, 1 to 500. |

`<CratisReadProtection>` is the build-time mode declaration. Arc 23 defaults to `On`; runtime configuration supplies an undeclared default or caps a declared mode, but cannot raise a declared mode. See [read protection](../chronicle/consistency/index.md).

## Environment variables Arc reads

| Variable | Effect |
| --- | --- |
| `DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT` | Development fallback when no host environment is available; `DOTNET_ENVIRONMENT` takes precedence. See [Environment detection](#how-arc-detects-development). |

## How Arc detects Development

`ArcOptions.ExposeExceptionDetails` uses the host's `IHostEnvironment.IsDevelopment()` when registered. Without a host it falls back to `DOTNET_ENVIRONMENT`, then `ASPNETCORE_ENVIRONMENT`. A Development host therefore gets exception details even when its environment was set in code. An explicit configuration or options value always wins:

```json
{ "Cratis": { "Arc": { "ExposeExceptionDetails": true } } }
```

Discovery access and query-health access also check the host environment first and use the same environment-variable fallback when no host is registered. Arc's service-provider scope validation also follows the host environment. Do not switch a deployed host to Development to restore access.

## `AddCratis()` authenticates nobody until you opt in

`AddCratis()` registers the Microsoft Identity Platform header adapter. With `TrustForwardedIdentityHeaders` at its default of `false`, that adapter ignores the `x-ms-client-principal*` headers, so every request is anonymous and endpoints that require a user return 401. The host logs a warning on every startup naming the setting.

Set `Cratis:Arc:TrustForwardedIdentityHeaders` to `true` only when every request comes through an ingress that authenticates callers and replaces those headers. Otherwise register an authentication scheme that validates callers. See [Microsoft Identity](../asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).
