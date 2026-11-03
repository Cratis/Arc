---
title: Generated values
description: Give command handlers server-owned identifiers, codes and secrets without adding them to the request contract.
---

A registration command needs a new customer identity chosen by the server, not supplied in the
request. Calling `CustomerId.New()` inside the handler hides that choice from a spec that needs to pin
the value. Adding a `CustomerId` command property gives the client the choice instead.

Mark a Handle parameter with `[GeneratedValue]` to make it server-owned. Arc generates and validates
it before calling `Handle()`, then captures the accepted value for specs. It is not part of the request
contract. A standalone handler can return the identifier without appending an event or using Chronicle.

The examples follow the shop from the [Generated values tutorial](/generated-values/) and
[Generated Values Shop sample](https://github.com/Cratis/Samples/tree/main/GeneratedValues/Shop).
Start with the concept and handler below; the [plain-Guid example](#a-plain-guid-works-too) then shows generation without an
event-store dependency. For the underlying UUID, ULID, TypeID, NanoID, and bytes strategies, use
[Generating values](/fundamentals/csharp/generating-values/) in Fundamentals.

## Declare a server-owned identifier

Define the strategy on the concept before marking a handler parameter. Here `CustomerId` is `cus_`
followed by a lowercase ULID. It implements `IGeneratable<TSelf>` from Cratis.Fundamentals 7.23.0 or
later; its static `New()` method supplies the value:

```csharp title="Shop/Common/CustomerId.cs"
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Shop.Common;

public record CustomerId(string Value) : EventSourceId<string>(Value), IGeneratable<CustomerId>
{
    public static implicit operator CustomerId(string value) => new(value);

    public static CustomerId New() => new($"cus_{GenerateValue.Ulid().ToLowerInvariant()}");
}
```

The command then asks for it as a Handle parameter. The request carries only the customer's name:

```csharp title="Shop/Customers/Registration/RegisterCustomer.cs"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Concepts;
using Shop.Common;

namespace Shop.Customers.Registration;

public record CustomerName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator CustomerName(string value) => new(value);
}

[Command]
public record RegisterCustomer(CustomerName Name)
{
    public (CustomerId, EventForEventSourceId) Handle([GeneratedValue("customerId")] CustomerId customerId) =>
        (customerId, new EventForEventSourceId(customerId, new CustomerRegistered(Name)));
}

[EventType]
public record CustomerRegistered(CustomerName Name);
```

Arc calls `CustomerId.New()` before invoking `Handle()`. Because `customerId` is a handler parameter
rather than a command property, neither the request body nor the generated TypeScript proxy contains
it. The proxy for `RegisterCustomer` has only the `name` property. The handler returns the allocated
identifier as the response and appends `CustomerRegistered` to that identifier. Check the command's
final result before using its response; having generated an identifier does not mean execution succeeded.

This example uses Arc's Chronicle integration for `EventForEventSourceId`. Generation does not depend on
it: a handler without Chronicle can return the generated value alone.

### A plain Guid works too

An identifier that implements `IGeneratable<TSelf>` over a `Guid` follows the same shape. These
declarations allocate an identifier; they do not persist a cart or append an event, and the
[migration guide](../../../upgrading/generated-values.mdx) reuses them:

```csharp title="Shop/Carts/Starting/CartId.cs and StartCart.cs"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
{
    public static CartId New() => new(GenerateValue.Uuid());
}

[Command]
public record AllocateCartId
{
    public CartId Handle([GeneratedValue("cart")] CartId cartId) => cartId;
}
```

Only non-nullable types that implement `IGeneratable<TSelf>` or `IGeneratableInContext<TSelf>` for
themselves are valid. A raw `Guid`, a nullable concept, duplicate names, or a generated parameter on
`Provide()` fails at handler construction.

## Generate with the command's time and tenant

Use the context-aware contract when a strategy needs the command's time or tenant. If `OrderId.New()`
reads the clock itself, values created during one command can carry different times and a spec cannot
pin the clock through Arc's generation context. Implement `IGeneratableInContext<TSelf>` from
Cratis.Fundamentals 7.24.0 or later to receive that context:

```csharp title="Shop/Orders/Placing/PlaceOrder.cs"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Concepts;
using Shop.Common;

namespace Shop.Orders.Placing;

public record OrderId(string Value) : EventSourceId<string>(Value), IGeneratableInContext<OrderId>
{
    public static OrderId New(GenerationContext context) => new(GenerateValue.TypeId("order", context));
}

[Command]
public record PlaceOrder(CustomerId CustomerId)
{
    public (OrderId, EventForEventSourceId) Handle([GeneratedValue("orderId")] OrderId orderId) =>
        (orderId, new EventForEventSourceId(orderId, new OrderPlaced(CustomerId)));
}

[EventType]
public record OrderPlaced(CustomerId CustomerId);
```

Arc passes `CommandContext.ReceivedAt` as `GenerationContext.Now`, so every generated value uses the
command's captured received time instead of a second clock read. It also maps
`ITenantIdAccessor.Current` to `GenerationContext.Tenant`: `TenantId.NotSet` and `TenantId.Default` become
`GenerationContext.DefaultTenant` (the zero-GUID string), and named tenants use their `Value`.

The shop's `OrderId` takes its time from the command and has **no tenant in the value**. A TypeID has no
tenant slot, and `GenerateValue.TypeId("order", context)` reads only `context.Now`. When a value really
must carry the tenant, declare a separate concept, such as a `TenantOrderId` built as
`$"{context.Tenant}-{GenerateValue.Ulid(context)}"`. Namespaces already isolate tenants in most
applications, so this is rarely needed. See
[Context-dependent values](/fundamentals/csharp/generating-values/context/) for the contract.

Three rules to keep in mind:

- If a type implements both interfaces, **Arc always calls the context form**. Its plain `New()` is never
  selected by Arc, and [ARC0027](../code-analysis/ARC0027.md) warns about it.
- A default `ReceivedAt` throws `MissingGenerationTime`, even when a custom strategy ignores time. A manually
  constructed command context must supply its received time; Arc never substitutes `UtcNow`.
- Scenario pins bypass generation, but still pass through validation.

## Keep a secret out of the causation chain

For the shop's `ApiKey`, the strategy uses 32 random bytes. Consult
[Secrets and tokens](/fundamentals/csharp/generating-values/secrets-and-tokens/) before selecting or
shortening `Bytes(32)`. Generation chooses the secret; the following Chronicle-specific example addresses
where the application sends it, which is a separate security concern.

```csharp title="Shop/Customers/ApiKeys/ApiKey.cs"
using Cratis.Arc.Chronicle.Commands;
using Cratis.Concepts;

namespace Shop.Customers.ApiKeys;

[NotAudited]
public record ApiKey(string Value) : ConceptAs<string>(Value), IGeneratable<ApiKey>
{
    public static ApiKey New() => new(GenerateValue.Bytes(32, ValueEncoding.Base64Url));
}
```

Chronicle records a command's property values on the causation of every event it appends.
`[NotAudited]` on the concept leaves any command property of that type out of the causation chain, and
that is **all** it does. It does not protect event payloads, command responses or logs.
`IssueApiKey` returns a generated key as its response. Its event goes to the customer, not to the key:

```csharp title="Shop/Customers/ApiKeys/IssueApiKey.cs"
[Command]
public record IssueApiKey(CustomerId CustomerId)
{
    public (ApiKey, EventForEventSourceId) Handle([GeneratedValue("apiKey")] ApiKey apiKey) =>
        (apiKey, new EventForEventSourceId(CustomerId, new ApiKeyIssued()));
}

[EventType]
public record ApiKeyIssued;
```

This example returns the key in the response and omits it from `ApiKeyIssued`. It does not implement
one-time retrieval, guarantee delivery, or prevent the caller from logging or retaining the response.
The `ApiKey` is not an event source id: the event is appended to the `CustomerId`. Keep the key out of
event payloads and logs in application code; use Chronicle compliance (`[Encrypted]` and friends) for
data that must be stored.

`[NotAudited]` earns its place on commands that receive the key as input. The shop's `RevokeApiKey`
takes an `ApiKey` property, and a spec confirms its value does not appear on the causation of
`ApiKeyRevoked`. A generated Handle parameter is not a command property, so it never reaches the causation
chain in the first place.

## Choose a strategy

Match the value to its job, then read the linked page before shortening anything:

| Value | Strategy | Why |
| --- | --- | --- |
| `CustomerId`, `CartId` | `Ulid()` with a prefix, or `Uuid()` | Identifier; plenty of random bits |
| `OrderId` | `TypeId("order", context)` | Names its type, sorts by the command's time |
| `InviteCode` | `NanoId(10, "23456789ABCDEFGHJKLMNPQRSTUVWXYZ")` | People read it aloud; only 50 random bits, so it is a generated property, never an identifier; back it with a uniqueness check where collisions matter |
| `ApiKey` | `Bytes(32)` | Secret, 256 bits |

The decision flow, collision tables and entropy guidance are in
[Choosing a strategy](/fundamentals/csharp/generating-values/choosing-a-strategy/).

## Names and resolution

Use an explicit name when specs must survive a parameter rename. By default, `[GeneratedValue]` uses
the C# parameter name; `[GeneratedValue("cart")]` fixes the name independently. Names are case-sensitive,
non-empty, and unique within the command. Specs use that same name to pin a value.

Arc generates values only after `Provide()` control results allow the command to continue. A value
returned by `Provide()`, or registered in dependency injection, never overrides a generated parameter.
Unmarked parameters keep their normal Provide and dependency injection behavior.

The Arc source generator registers a typed factory for each accessible generatable type. Commands compiled
without it can use a cached reflection fallback under the JIT. When dynamic code is unavailable, a missing
factory fails at handler construction with `MissingGeneratedValueFactory`, naming the command and
parameter. This factory path alone does not make the whole application NativeAOT-compatible.

## Validation and captures

A generated value must satisfy its concept rules before the handler receives it. Arc resolves the
concept's discoverable validator from the command's scoped provider and runs it asynchronously with
the command's cancellation token. Any failing rule, including Warning or Information, rejects either a
generated or fixture-pinned value with `GeneratedValueRejected`.

Caller severity thresholds do not turn these failures into advice. A server-selected value breaking a
concept rule is a server defect, not invalid request input: the command result contains an exception,
not a 400 validation failure. Screenplay's reference makes the same distinction.

`CommandContext.GeneratedValues` exposes validated captures in parameter resolution order. Each entry has a
zero-based `Order`, a `Name`, its declared `Type`, its `Value`, and the immutable `Freshness` policy.
`Get<CustomerId>("customerId")`
retrieves the latest matching capture and fails if none exists. Context copies retain the same captures.

:::caution[Do not log or trace generated values]
Validator failure text can interpolate the value. `GeneratedValueRejected.Failures` keeps that text apart
from the safe exception message, and rejected values are not captured. Keep both out of logs, and treat
captures of secrets like the secret.
:::

## When not to use it

`[GeneratedValue]` is for new values that only the server should decide. It is the wrong tool when:

- **The client chooses the identifier.** A customer to update, an order to cancel and any reference the
  caller supplies belong on command properties. Marking them generated would discard what the caller sent.
- **You need idempotency.** A repeated invocation can generate a different value. Retrying a successful
  registration can therefore create another customer, depending on the handler and storage. Accept and
  enforce a client-supplied idempotency key when retries must not repeat the change.
- **You need gapless or human-sequenced numbers.** Random and time-ordered values are unique in practice,
  not consecutive. An invoice number with no gaps needs a counter, not a strategy.
- **The value is an authorization decision.** A generated value does not exist during authorization or
  validation, so a rule cannot read it. A short code such as `InviteCode` is not proof of identity either:
  pair it with an expiry and a check.
- **You need a service or a read model.** Inject those as ordinary Handle dependencies.

Enforce storage uniqueness where the operation requires it; generating a value does not prove it is
absent from storage. Time-ordered values, UUIDv7 included, reveal their creation time and are not
monotonic within a millisecond.

## Guard a generated append target

With Arc's Chronicle integration, `GeneratedValueFreshness.Guarded` is the default. Returned events and
aggregate mutations targeting a generated identifier require the whole event source to be empty, regardless
of stream or event type. Occupancy produces `ConcurrencyViolation` with `ReasonDetail = "GeneratedIdentifierCollision"`;
Arc does not rerun `Handle()`.

`[GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)]` opts out while keeping generation and
validation. Values used only in a response or payload add no scope. Direct user calls to unit-of-work
append APIs inside `Handle()` are not automatically guarded. See
[Fresh identifiers](../chronicle/commands/fresh-identifiers.md) for supported paths, explicit-scope merging,
version requirements and the direct-write workaround.

## Analyzers

Arc checks generated declarations in the IDE and at compile time:
[ARC0022](../code-analysis/ARC0022.md) rejects invalid or nullable types,
[ARC0023](../code-analysis/ARC0023.md) rejects attributes outside command Handle parameters,
[ARC0024](../code-analysis/ARC0024.md) rejects empty or duplicate names, and
[ARC0025](../code-analysis/ARC0025.md) warns when `Provide()` supplies a generated type.
[ARC0027](../code-analysis/ARC0027.md) warns about dual generation interfaces, and
[ARC0028](../code-analysis/ARC0028.md) offers migration from inline generation to a parameter.
Startup validation remains in place for commands compiled without the analyzers.

## Custom factories in a generated model

Use this section when recovering a Screenplay model from the C# application, not when choosing how a
standalone handler receives its value. Runtime generation and model recovery have different limits.

Arc executes your concept's generation contract at runtime; Screenplay recovery recognizes only the
[documented factory forms](../generating-a-screenplay.md#recovering-generation-strategies). A custom
helper or external dependency can be valid Arc code without a recoverable Screenplay strategy. Self-contained
String bodies, including multi-statement factories using `context.Now`, tenant formatting and
`GenerateValue.*(context)`, recover as inline `generate rule` bodies in ESM v21. The Cratis C# target binds
ValueGeneration to Fundamentals' `Cratis.Concepts.GenerationContext`; passing that context to a Fundamentals
API is target-bound, not a promise of portability to other providers. Rule bodies never become generated
identifiers because their entropy is unknown. Literal `GenerateValue.Pattern` calls and signed
`GenerateValue.Integer` ranges also recover in v21; an `int` factory needs one in-range cast, and unsigned
backings are refused. `SP0073` names that limitation; it does not change your runtime factory or make the
value caller-supplied.

The generated model keeps independent parameters and behavior. It withholds a whole event production
when a required payload member or explicit destination depends on the opaque value; an optional payload
dependency removes only that mapping. Dependent responses and operations stay in code too. When no event
production remains, the model keeps a handler file reference, which is not executable Screenplay behavior.
Enabling authoring-only constructs or raising the executable version cap does not recover an unknown
factory. Keep the original handler and use the diagnostics to identify what the partial model cannot run.

## Validated generation in a Screenplay model

A concept validator does not by itself prevent recovery. An empty validator keeps the existing generation
contract; a String-backed concept with completely recoverable rules can carry both `generate` and
`validate` in ESM v20. Screenplay checks that the strategy and rules agree. Proven rules are admitted;
non-opaque unproven rules stay in the model with Information `SP0078` (`PLAY0689`). Refuted rules withhold
generation and dependent claims with `SP0073` and the `PLAY0688` witness, while preserving validation.

Unreadable, conditional or opaque validation stays outside executable generation. UUID validator rules
such as Guid `NotEmpty` also remain outside Screenplay's declarative algebra: recovery preserves the
original validator through a source attachment rather than pretending UUID is text. This does not change
Arc's runtime validation behavior. A cap below ESM v20 withholds validated generation, not its rules, in
both output modes. ESM v21 additionally recovers unconditional String `Must(v => GenerateValue.HasValidChecksum(v, Checksum.Luhn))`
rules, with `Mod11` and `Mod97` supported too. `None` and conditional checksum predicates are not recovered.
Below v21, generation and its dependents are withheld while the checksum validator remains source-backed;
rules are never stripped to fit an older cap. See [Validated generation](../generating-a-screenplay.md#validated-generation-and-esm-v20)
for the admission and fallback details.

## Next steps

- [Pin generated values in command specs](../testing/generated-values.md) so a spec can choose the value or assert its time part.
- [Migrate generated identifiers](../../../upgrading/generated-values.mdx) when handlers still call `New()` inside `Handle()`.
- [Generating a Screenplay](../generating-a-screenplay.md#recovering-generation-strategies) to see which strategies Arc recovers into a model.
- [Generated values, from Screenplay to running app](/generated-values/) for the whole path across products.
- Inspect captures through the [command context](command-context.md).
