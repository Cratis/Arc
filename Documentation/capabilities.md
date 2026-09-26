---
title: Capability matrix
description: Which Arc capabilities exist in each backend implementation - C#, Kotlin, and Java - with the boundary of every partial answer.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

This matrix covers two backend implementations: [C# on ASP.NET Core](/arc/backend/csharp/), and
[Kotlin and Java on Spring Boot](/arc/backend/kotlin/). Use this page to check whether a
capability exists in the language you work in before you design around it.

:::note[Arc for TypeScript]
[Arc for TypeScript](/arc/backend/typescript/), a third implementation for Node.js, is in
source preview and is not in this matrix: a column here needs evidence behind every cell.
Its [capability reference](/arc/backend/typescript/reference/capabilities/) gives the status
of each capability, the spec or check that proves it, and where it deliberately differs from
Arc on .NET. It makes no full-parity claim.
:::

Each cell was checked against that implementation's source, public API, and tests, not
against its documentation. A cell says a capability exists. It does not say the two
implementations behave identically: where both have a capability but it differs on the wire,
[where the implementations differ](/arc/http-contract/#where-the-implementations-differ)
describes the difference. The Kotlin and Java evidence behind every row, including which test
proves it, is in the [JVM parity reference](/arc/backend/kotlin/reference/parity/).

## Reading the matrix

| Status | Meaning |
| --- | --- |
| Implemented | The capability exists in the current source and public API and is usable from this language. |
| Partial | Usable, with the boundary stated in the notes. |
| Not planned | Absent from source, and recorded as not planned for that implementation. |
| Not applicable | The capability belongs to another platform's host or toolchain. |

A capability one implementation has and the other has neither built nor ruled out is not
forced into these states. It is listed under
[not yet in every implementation](#not-yet-in-every-implementation).

### What the Java column counts

Kotlin and Java share one runtime. The Java column counts capabilities usable through
Java-callable APIs; language-specific verification limits are stated separately. Java uses
annotations on Java types, `CompletionStage` and `Flow.Publisher` returns, static query
methods, and the `Blocking*` and `Async*` contracts rather than implementing suspending methods.

Not every Java contract is discovered the same way:

- **Collected as Spring beans directly:** `AsyncAuthenticationHandler`,
  `AsyncIdentityDetailsProvider`, `AsyncUsersProvider`, `AsyncTenantsProvider`,
  `ConceptValidator`, and `BlockingQueryRendererFor`, `BlockingReadModelInterceptor`,
  `BlockingObservableQueryEmissionGuard`, and `BlockingReadModelForCommandResolver`, which
  extend the Kotlin contracts.
- **Run only through an adapter bean you declare:** command and query filters, authorization
  filters and named policies, command and query validators, command execution scopes, and
  command response value handlers. Wrap the Java implementation in the matching
  `Blocking*Adapter` or `Async*Adapter` from `io.cratis.arc.java` and return it from a `@Bean`
  method. A class that implements `BlockingCommandFilter` without that bean is never called.
  See [pipeline filters](/arc/backend/kotlin/guides/pipeline-filters/).

## Commands

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| Model-bound commands | Implemented | Implemented | Implemented | A command type with a `Handle`/`handle` method. [C#](backend/csharp/commands/model-bound/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/commands/) |
| Asynchronous handlers | Implemented | Implemented | Implemented | C# `Task`/`ValueTask`, Kotlin `suspend`, Java `CompletionStage`. |
| Provided values | Implemented | Implemented | Implemented | `Provide`/`provide` loads data after filters pass and hands it to the handler. [C#](backend/csharp/commands/model-bound/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/commands/) |
| Command keys | Implemented | Implemented | Implemented | C# `ICanProvideKeyForCommand`; JVM `@CommandKey` and `CommandKeyProvider`. [C#](backend/csharp/commands/command-context.md) · [Kotlin and Java](/arc/backend/kotlin/guides/command-keys/) |
| Several return values | Implemented | Implemented | Implemented | C# tuples; JVM `Pair`, `Triple`, `CommandProvidedValues`, and `CommandResponseValues`. Exactly one value may reach the client on the JVM. [C#](backend/csharp/commands/response-value-handlers.md) |
| Alternative return values | Implemented | Implemented | Implemented | C# `OneOf` values; JVM `ArcOneOf`. Java can build an `ArcOneOf` through its static `of` and `alternative` factories, but no Java-authored test returns one. Generating named union types (`@GenerateOneOf`) is not planned on the JVM. |
| Response value handlers | Implemented | Implemented | Implemented | Java needs an adapter bean. [C#](backend/csharp/commands/response-value-handlers.md) |
| Validate without executing | Implemented | Implemented | Implemented | `POST <command-route>/validate`. [C#](backend/csharp/commands/validation.md) · [Kotlin and Java](/arc/backend/kotlin/guides/validation/) |
| Command filters | Implemented | Implemented | Implemented | Java needs an adapter bean. [C#](backend/csharp/commands/command-filters.md) · [Kotlin and Java](/arc/backend/kotlin/guides/pipeline-filters/) |
| Command execution scopes | Implemented | Implemented | Implemented | Java needs an adapter bean. [C#](backend/csharp/commands/command-execution-scopes.md) · [Kotlin and Java](/arc/backend/kotlin/guides/execution-scopes/) |
| Calling the pipelines from code | Implemented | Implemented | Implemented | Java uses `BlockingCommandPipeline`, `BlockingQueryPipeline`, and `JavaAsyncScope`; blocking calls need explicit per-call or constructor-bound options. [C#](backend/csharp/commands/command-pipeline.md) · [Java](/arc/backend/kotlin/get-started/java/#call-pipelines-from-an-imperative-java-service) |
| Controller-based commands and queries | Implemented | Not planned | Not planned | The JVM generates model-bound Spring MVC endpoints only. [C#](backend/csharp/commands/controller-based.md) |

## Queries

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| Model-bound queries | Implemented | Implemented | Implemented | Static methods on a read model; Kotlin uses `@JvmStatic` companion methods. [C#](backend/csharp/queries/model-bound/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/queries/) |
| HTTP `QUERY` method | Implemented | Implemented | Implemented | [C#](backend/csharp/queries/using-the-http-query-method.md) · [Kotlin and Java](/arc/backend/kotlin/guides/queries/) |
| Observable queries | Implemented | Implemented | Implemented | C# `ISubject<T>`/`IObservable<T>`; Kotlin `Flow`/`StateFlow`; Java `Flow.Publisher` and `ObservableState`. [C#](backend/csharp/queries/model-bound/observable-queries.md) · [Kotlin and Java](/arc/backend/kotlin/guides/observable-queries/) |
| Observable transports | Implemented | Implemented | Implemented | HTTP snapshot, direct SSE and WebSocket, and the multiplexed hubs. The JVM transport code is shared; its SSE and WebSocket runtime tests run against the Kotlin sample. Snapshot readiness differs; see [the HTTP contract](http-contract.md#observable-http-snapshot-readiness). |
| Paging and sorting | Implemented | Implemented | Implemented | The result shapes that get paged differ. C# pages and sorts an `IQueryable` result through [`QueryableQueryRenderer`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Queries/QueryableQueryRenderer.cs). The JVM's default fallback pages and sorts `Iterable` results in memory when no custom renderer matches; custom renderers own their paging and sorting. `QueryPage` and Spring Data `Page` results pass through; a returned array is not sorted or paged, and its paging totals stay at zero. [C#](backend/csharp/queries/model-bound/paging.md) · [Kotlin and Java](/arc/backend/kotlin/guides/queries/) |
| Query filters | Implemented | Implemented | Implemented | Java needs an adapter bean. [C#](backend/csharp/queries/query-pipeline.md) · [Kotlin and Java](/arc/backend/kotlin/guides/pipeline-filters/) |
| Renderers, read-model interceptors, and emission guards | Implemented | Implemented | Implemented | [C#](backend/csharp/queries/read-model-interception.md) · [Kotlin and Java](/arc/backend/kotlin/guides/queries/) |
| Services in query methods | Implemented | Implemented | Implemented | Service parameters are injected and never become client arguments. [C#](backend/csharp/queries/model-bound/dependency-injection.md) · [Kotlin and Java](/arc/backend/kotlin/guides/queries/) |
| Spring Data `Pageable`, `Sort`, and `Page` | Not applicable | Implemented | Implemented | [Kotlin and Java](/arc/backend/kotlin/guides/spring-data/repositories/) |
| Query health endpoint | Implemented | Implemented | Implemented | `/.cratis/queries/health`. Its authentication differs; see [the HTTP contract](http-contract.md#query-health-is-anonymous-on-net). [C#](backend/csharp/queries/query-health.md) |

## Validation

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| Command and query validators | Implemented | Implemented | Implemented | Java needs an adapter bean. [C#](backend/csharp/commands/validation.md) · [Kotlin and Java](/arc/backend/kotlin/guides/validation/) |
| Validation severity | Implemented | Implemented | Implemented | Warnings and information alongside errors, and treating warnings as errors. [C#](backend/csharp/commands/validation-severity-filtering.md) · [Kotlin and Java](/arc/backend/kotlin/guides/validation/) |
| Concept validators | Implemented | Implemented | Implemented | One rule for a concept, applied wherever the concept appears. [C#](backend/csharp/queries/validation.md) · [Kotlin and Java](/arc/backend/kotlin/guides/validation/) |
| Annotation rules in generated proxies | Implemented | Implemented | Implemented | C# DataAnnotations; JVM Jakarta Bean Validation, which covers more annotations. [C#](backend/csharp/proxy-generation/validation.md) · [Kotlin and Java](/arc/backend/kotlin/reference/validation/) |
| Validator rules shared with the client | Implemented | Implemented | Implemented | Each side sends only the rules it recognizes. C# extracts the FluentValidation rules its proxy generator knows; the JVM `FluentModelValidator<T>` supports thirteen literal rules and no warning severity. [C#](backend/csharp/proxy-generation/validation.md) · [Kotlin and Java](/arc/backend/kotlin/guides/validation/) |
| Opting one member out of validation | Partial | Implemented | Implemented | C# [`[IgnoreValidation]`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc/IgnoreValidationAttribute.cs) applies to a controller class or action, not to a model-bound artifact or one member; one member can skip concept validators with [`IgnoreConceptRules()`](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Arc.Core/Validation/IConceptRuleBuilder.cs). JVM `@IgnoreValidation` skips one member and everything below it. [Kotlin and Java](/arc/backend/kotlin/reference/validation/) |

## Security and identity

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| Anonymous, authenticated, and role authorization | Implemented | Implemented | Implemented | An operation's own declaration replaces its type's. [C#](backend/csharp/authorizing-commands-and-queries.md) · [Kotlin and Java](/arc/backend/kotlin/guides/security/) |
| Named policies and authentication schemes | Implemented | Implemented | Implemented | C# runs scoped asynchronous native policies on either host and ASP.NET Core policies on that host. Actual scheme authentication requires ASP.NET Core and an Arc-owned execution scope; standalone Core rejects scheme requirements. The JVM checks the caller's already-captured scheme rather than authenticating another scheme. Java policies need an adapter bean. [C# policies](backend/csharp/core/authorization.md#register-a-named-policy) · [C# scheme boundaries](backend/csharp/asp-net-core/authorization.md#policy-based-authorization) · [Kotlin and Java](/arc/backend/kotlin/guides/security/) |
| Authentication handlers | Implemented | Implemented | Implemented | Java implements `AsyncAuthenticationHandler`. [C#](backend/csharp/core/authentication.md) · [Kotlin and Java](/arc/backend/kotlin/guides/security/) |
| Identity details and `/.cratis/me` | Implemented | Implemented | Implemented | [C#](backend/csharp/identity/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/security/) |
| Microsoft identity platform headers | Implemented | Implemented | Implemented | `x-ms-client-principal`. Off by default on the JVM; enabling it also requires an application `ArcPlatformIdentityTrust` bean because the default trusts no request. [C#](backend/csharp/asp-net-core/microsoft-identity.md) · [Kotlin and Java](/arc/backend/kotlin/guides/security/) |
| Development users and tenants | Implemented | Implemented | Implemented | `/.cratis/users` and `/.cratis/tenants`. [C#](backend/csharp/identity/development-and-topologies.md) · [Kotlin and Java](/arc/backend/kotlin/guides/development-users-and-tenants/) |
| Tenant resolution | Implemented | Implemented | Implemented | Fixed, header, query string, claim, subdomain, and development resolvers. Enforcement differs; see [the HTTP contract](http-contract.md#tenant-resolution-strategy-and-enforcement). [C#](backend/csharp/tenancy/resolvers.md) · [Kotlin and Java](/arc/backend/kotlin/guides/ambient-tenancy/) |

## Persistence

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| MongoDB | Implemented | Implemented | Implemented | C# `Cratis.Arc.MongoDB`; JVM Spring Data MongoDB. [C#](backend/csharp/mongodb/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/spring-data/) |
| Relational databases | Implemented | Implemented | Implemented | C# Entity Framework Core; JVM Spring Data JPA. [C#](backend/csharp/entity-framework/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/spring-data/) |
| Observable queries from database changes | Implemented | Implemented | Implemented | C# observes MongoDB collections and EF Core sets. The JVM uses MongoDB change streams, which need a replica set or sharded cluster, and explicit in-process JPA notifications. [C#](backend/csharp/mongodb/observing-collections.md) · [Kotlin and Java](/arc/backend/kotlin/guides/spring-data/observable-snapshots/) |
| Read models in command handlers | Implemented | Implemented | Implemented | Arc resolves a read model by command key and hands it to the handler. [C#](backend/csharp/chronicle/read-models/other-providers.md) · [Kotlin and Java](/arc/backend/kotlin/guides/spring-data/repositories/) |

## Chronicle integration

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| Events returned from commands | Implemented | Implemented | Implemented | Appended only when the command succeeds. [C#](backend/csharp/chronicle/commands/events.md) · [Kotlin and Java](/arc/backend/kotlin/guides/chronicle/) |
| Concurrency scopes | Implemented | Implemented | Implemented | [C#](backend/csharp/chronicle/commands/concurrency.md) · [Kotlin and Java](/arc/backend/kotlin/guides/chronicle/) |
| Chronicle read models in commands and queries | Implemented | Implemented | Implemented | [C#](backend/csharp/chronicle/read-models/injecting-into-commands.md) · [Kotlin and Java](/arc/backend/kotlin/guides/chronicle/) |
| Commands from reactors | Implemented | Implemented | Implemented | Both let a reactor declare the system roles its commands run with through `ExecuteCommandsAsSystem`. A JVM reactor hands command values to `ChronicleCommandSideEffectHandler`. [C#](backend/csharp/chronicle/reactors/command-side-effects.md) · [Kotlin and Java](/arc/backend/kotlin/guides/chronicle/) |
| Chronicle command scenarios | Implemented | Implemented | Implemented | An in-memory event log; no Chronicle kernel runs. [C#](backend/csharp/testing/chronicle.md) · [Kotlin and Java](/arc/backend/kotlin/guides/testing/) |

## Testing

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| In-process scenarios | Partial | Implemented | Implemented | C# has command and snapshot query scenarios, but no observable-query scenario. The JVM has command, query, and observable query scenarios; Java uses the `Blocking*` and `Async*` scenario classes. [C# commands](backend/csharp/testing/command-scenario.md) · [C# queries](backend/csharp/testing/query-scenario.md) · [Kotlin and Java](/arc/backend/kotlin/guides/testing/) |

## Tooling and clients

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| TypeScript proxies for commands and queries | Implemented | Implemented | Implemented | [C#](backend/csharp/proxy-generation/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/typescript-proxies/) |
| TypeScript proxies for observable queries | Implemented | Implemented | Implemented | [C#](backend/csharp/proxy-generation/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/typescript-proxies/) |
| Concepts | Implemented | Implemented | Implemented | `ConceptAs<T>` crosses the wire as its underlying value. [C#](backend/csharp/proxy-generation/type-mapping.md) · [Kotlin and Java](/arc/backend/kotlin/reference/annotations/) |
| Polymorphic derived types | Implemented | Implemented | Implemented | A `_derivedTypeId` discriminator. A JVM hierarchy that arrives only as a dependency binary needs a `DerivedTypeRegistrar`. [Kotlin and Java](/arc/backend/kotlin/reference/annotations/) |
| `DateOnly`, `TimeOnly`, and `Guid` in proxies | Implemented | Implemented | Implemented | JVM `Duration` is ISO-8601 text, not `TimeSpan`. [C#](backend/csharp/proxy-generation/type-mapping.md) · [Kotlin and Java](/arc/backend/kotlin/guides/typescript-proxies/) |
| Documentation comments in proxies | Implemented | Implemented | Implemented | C# XML documentation; Kotlin KDoc; Java Javadoc. Which declarations each side documents has not been compared. |
| Mapping types to TypeScript packages | Implemented | Implemented | Implemented | C# maps by assembly and by type; the JVM maps by package and by type. [C#](backend/csharp/proxy-generation/Configuration/assembly-package-mappings.md) · [Kotlin and Java](/arc/backend/kotlin/guides/typescript-proxies/) |
| Types outside commands and queries | Implemented | Implemented | Implemented | C# library mode; JVM `@ExportedType` and identity details roots. [C#](backend/csharp/proxy-generation/Configuration/library-mode.md) · [Kotlin and Java](/arc/backend/kotlin/reference/annotations/) |
| Build-time diagnostics | Implemented | Implemented | Implemented | C# Roslyn analyzers; JVM KSP diagnostics, which also cover Java sources. [C#](backend/csharp/code-analysis/index.md) · [Kotlin and Java](/arc/backend/kotlin/reference/diagnostics/) |
| OpenAPI | Implemented | Implemented | Implemented | [C#](backend/csharp/open-api/index.md) · [Kotlin and Java](/arc/backend/kotlin/guides/openapi/) |
| Introspection endpoints | Implemented | Implemented | Implemented | `/.cratis/commands` and `/.cratis/queries`. Metadata depth differs; see [the HTTP contract](http-contract.md#introspection-metadata-depth). [C#](backend/csharp/introspection/index.md) |
| Screenplay generation | Implemented | Not planned | Not planned | [C#](backend/csharp/generating-a-screenplay.md) |

## Hosting

| Capability | C# | Kotlin | Java | Notes |
| --- | --- | --- | --- | --- |
| ASP.NET Core host | Implemented | Not applicable | Not applicable | [C#](backend/csharp/asp-net-core/index.md) |
| Spring Boot host | Not applicable | Implemented | Implemented | [Kotlin and Java](/arc/backend/kotlin/get-started/) |
| Standalone HTTP host outside ASP.NET Core or Spring Boot | Implemented | Not planned | Not planned | C# `ArcApplication`. [C#](backend/csharp/core/index.md) |
| Static files and SPA fallback | Implemented | Not planned | Not planned | Spring Boot serves static resources itself. [C#](backend/csharp/core/static-files.md) |
| `[FromRequest]` binding | Implemented | Not planned | Not planned | [C#](backend/csharp/asp-net-core/from-request.md) |
| Opting a type out of auto-registration | Implemented | Not applicable | Not applicable | C# `[IgnoreAutoRegistration]` excludes a `DbContext` from Entity Framework Core hookup. JVM entry points are opt-in annotations. [C#](backend/csharp/entity-framework/automatic-database-hookup.md) |
| Correlation IDs | Implemented | Implemented | Implemented | `X-Correlation-ID` on every request. Edge cases differ; see [the HTTP contract](http-contract.md#correlation-identifier-edge-cases). |
| Observability | Implemented | Implemented | Implemented | C# `ActivitySource` tracing; JVM Micrometer observations. [C#](backend/csharp/observability.md) · [Kotlin and Java](/arc/backend/kotlin/guides/observability/) |

## Not yet in every implementation

These C# capabilities have no Kotlin or Java counterpart in source, and no decision records
them as not planned for the JVM. They are listed here rather than marked not planned.

| Capability | Available in | Kotlin and Java |
| --- | --- | --- |
| [Command operations](backend/csharp/commands/operations/index.md) | C# | Not in source; no decision recorded. |
| [Aggregate roots](backend/csharp/chronicle/aggregates/index.md) | C# | Not in source; no decision recorded. |

## What this page leaves out

The JVM parity reference also tracks rows that describe how the JVM is verified rather than a
capability you adopt: the proxy differential gates, the runtime and HTTP conformance gates,
binary compatibility, runtime hardening, Kotlin ergonomics, and the Java adapters themselves.
It records cross-store transactions as not planned; neither implementation provides one.
Admission control and request limits are compared in
[the HTTP contract](http-contract.md#admission-control-and-request-limits).

## Related

- [HTTP contract](http-contract.md) - the wire protocol both implementations speak
- [JVM parity reference](/arc/backend/kotlin/reference/parity/) - the evidence behind the Kotlin and Java columns
- [TypeScript capability reference](/arc/backend/typescript/reference/capabilities/) - status and evidence for Arc for TypeScript
- [C# backend](/arc/backend/csharp/) and [Kotlin and Java backend](/arc/backend/kotlin/)
