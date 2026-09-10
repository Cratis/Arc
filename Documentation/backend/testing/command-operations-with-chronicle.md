---
title: Test operation recovery after a Chronicle rejection
description: Combine a returned event and command operation, then prove a real in-process uniqueness rejection prevents the event commit and triggers compensation.
---

**Goal:** prove that a returned operation can execute before Chronicle rejects the command's event transaction, and that Arc then compensates the operation without committing the rejected event.

This is the optional Chronicle counterpart to [testing standalone command operations](./command-operations.md). Keep it in a separate example project so adding Chronicle's discovered scenario extender does not change the standalone lesson's test environment.

## Add the Chronicle testing boundary

```bash
dotnet new classlib -n Onboarding
dotnet add Onboarding package Cratis.Arc.Chronicle
dotnet new xunit -n Onboarding.Specs
dotnet add Onboarding.Specs reference Onboarding/Onboarding.csproj
dotnet add Onboarding.Specs package Cratis.Arc.Chronicle.Testing
dotnet add Onboarding.Specs package Cratis.Arc
dotnet add Onboarding.Specs package Cratis.Specifications.XUnit
dotnet add Onboarding.Specs package NSubstitute
```

Use matching current Arc packages. Remove the template files. The example uses the real in-process Chronicle test event log and constraint evaluation, with a substitute only for the external reservation provider. It does not require a running Chronicle server.

The explicit `Cratis.Arc` test dependency supplies the Arc host assembly used by Chronicle's in-process kernel. This is a test-host dependency, not a requirement to expose an ASP.NET Core endpoint or to add Chronicle to standalone operation tests.

## Return an event and an operation

Add these declarations to the `Onboarding` project. The event's uniqueness constraint rejects a second onboarding with the same organization number. The reservation key identifies the attempted external work; it is separate from the Chronicle event-source identity.

```csharp title="Onboarding.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Concepts;

namespace Onboarding;

public record OrganizationNumber(string Value) : ConceptAs<string>(Value)
{
    public static readonly OrganizationNumber NotSet = new(string.Empty);
}

public record ReservationKey(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ReservationKey NotSet = new(Guid.Empty);
}

public interface IOnboardingReservations
{
    Task Reserve(ReservationKey reservationKey, CancellationToken cancellationToken);
    Task Cancel(ReservationKey reservationKey, CancellationToken cancellationToken);
}

public sealed record ReserveOnboardingCapacity(ReservationKey ReservationKey) : ICommandOperation
{
    public Task Execute(IOnboardingReservations reservations, CancellationToken cancellationToken) =>
        reservations.Reserve(ReservationKey, cancellationToken);

    public Task Compensate(IOnboardingReservations reservations, CancellationToken cancellationToken) =>
        reservations.Cancel(ReservationKey, cancellationToken);
}

[Command]
public record StartOnboarding(
    EventSourceId EventSourceId,
    OrganizationNumber OrganizationNumber,
    ReservationKey ReservationKey)
{
    public (OnboardingStarted Event, ReserveOnboardingCapacity Operation) Handle() =>
        (new OnboardingStarted(OrganizationNumber), new ReserveOnboardingCapacity(ReservationKey));
}

/// <summary>
/// Records that a partner started onboarding under its organization number.
/// </summary>
[EventType]
public record OnboardingStarted(
    [property: Unique("UniqueOrganizationNumber", "Organization number must be unique")]
    OrganizationNumber OrganizationNumber);
```

`Handle()` returns facts and work; it does not call the external provider. Arc enrolls the event, executes the operation, and completes the Chronicle transaction afterward. The event carries no event-source id; the command supplies it through Chronicle's existing identity convention.

The provider must honor the ownership and repeated-request rules described in [implementing an operation](../commands/operations/implementing.md#define-the-operations-inputs-and-provider). The substitute below cannot prove those external guarantees.

## Seed the conflict, then execute the command

Add this spec to `Onboarding.Specs`:

```csharp title="when_the_organization_is_already_onboarding.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding;
using Xunit;

namespace Onboarding.Specs;

public class when_the_organization_is_already_onboarding : Specification, IAsyncDisposable
{
    CommandScenario<StartOnboarding> _scenario = null!;
    readonly OrganizationNumber _organization = new("ORG-OPERATIONS");
    readonly ReservationKey _reservationKey = new(Guid.Parse("96ce8c18-1e67-4e44-b5e7-4fb2b2cb3b97"));
    EventSourceId _newPartner = null!;
    IOnboardingReservations _reservations = null!;
    CommandResult _result = null!;

    async Task Establish()
    {
        _scenario = new CommandScenario<StartOnboarding>();
        _newPartner = EventSourceId.New();
        _reservations = Substitute.For<IOnboardingReservations>();
        _reservations.Reserve(_reservationKey, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _reservations.Cancel(_reservationKey, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New())
            .Events(new OnboardingStarted(_organization));
    }

    async Task Because() =>
        _result = await _scenario.Execute(new StartOnboarding(_newPartner, _organization, _reservationKey));

    [Fact] void should_report_the_actual_constraint() =>
        _result.ShouldHaveConstraintViolationFor("UniqueOrganizationNumber");

    [Fact] void should_observe_forward_execution() =>
        _scenario.ShouldHaveExecutedOperation<ReserveOnboardingCapacity>();

    [Fact] void should_observe_compensation() =>
        _scenario.ShouldHaveCompensatedOperation<ReserveOnboardingCapacity>();

    [Fact] void should_call_the_provider_in_order() => Received.InOrder(() =>
    {
        _ = _reservations.Reserve(_reservationKey, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_reservationKey, Arg.Any<CancellationToken>());
    });

    [Fact] void should_report_known_noncommit() =>
        _result.Recovery!.CommitDisposition.ShouldEqual(CommandCommitDisposition.NotCommitted);

    [Fact] void should_report_observed_recovery_completion() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.Completed);

    [Fact] async Task should_not_persist_the_rejected_event() =>
        (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();

    async Task Destroy() => await ((IAsyncDisposable)this).DisposeAsync();

    ValueTask IAsyncDisposable.DisposeAsync() => _scenario.DisposeAsync();
}
```

The explicit `IAsyncDisposable` implementation expresses resource ownership to SDK analyzers. `Destroy()` connects it to Specifications' cleanup lifecycle and awaits the scenario's asynchronous disposal.

Run:

```bash
dotnet test Onboarding.Specs
```

The constraint assertion checks a real configured constraint, not a mock that merely reports `NotCommitted`. The provider calls and operation assertions establish that work ran before rejection and that its compensator returned. The event-log assertion establishes that the rejected source received no event.

## Keep the guarantees separate

This spec proves the in-process command/Chronicle integration for a **known rejection**. It does not simulate an uncertain commit acknowledgment, and it does not prove a production provider's cancellation or the transport behavior of a remote Chronicle server.

Do not replace the real constraint with a fabricated failed `CommandResult` and claim the same coverage. Conversely, use targeted integration/provider tests for uncertain outcomes instead of adding a real network dependency to every command decision spec.

For the other side of the boundary, execute with a previously unused organization number and assert successful event persistence, completed operation execution, `Committed` disposition, and no provider cancellation. A later presentation failure is not permission to reverse an already-committed business action.

Continue with [Chronicle transactional commands](../chronicle/commands/transactional-commands.md) for transaction boundaries and [operation recovery](../commands/operations/reference.md#commit-and-recovery) for conservative handling of committed or unknown outcomes.
