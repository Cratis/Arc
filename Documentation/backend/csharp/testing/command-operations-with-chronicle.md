---
title: Cratis Arc operation recovery after Chronicle rejection
description: Test that a real Chronicle uniqueness rejection prevents an event commit and triggers compensation for a Cratis Arc command operation.
---

Test a Cratis Arc command with Cratis Chronicle when you need to prove recovery after an event commit is rejected. This lesson lets an external reservation run, then checks that a real uniqueness constraint rejects the event and Arc calls the compensator to cancel the reservation.

Start with [standalone command operation tests](./command-operations.md) if you only need to check operation execution and compensation. Chronicle integration is optional. Keep this example in a separate project because its automatically discovered Chronicle extension changes the scenario's test environment.

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

Use matching current Arc packages and remove the template files. Chronicle's in-process event log evaluates the uniqueness constraint; only the external reservation provider is substituted. You do not need a running Chronicle server.

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

`Handle()` declares the event and reservation work without calling the provider. Arc adds the event to the command's transaction, executes the operation, and then completes the transaction. That order lets the spec exercise recovery after the reservation runs. The command's `EventSourceId` identifies the history that receives the event; the event does not repeat the id in its payload.

The provider must honor the ownership and repeated-request rules described in [implementing an operation](../commands/operations/implementing.md#define-the-operations-inputs-and-provider). The substitute below cannot prove those external guarantees.

## Seed the conflict, then execute the command

Add this spec to `Onboarding.Specs`. Seed a different event source with the same organization number so the new command reaches a real uniqueness conflict at commit time:

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

Read the assertions as separate checks. The named constraint identifies why the commit failed. The ordered provider calls show that reservation ran before cancellation, and the operation assertions record execution and compensation. Finally, the event-log assertion checks that the new partner received no event. A failed result alone would not establish that sequence.

## Keep the guarantees separate

This spec covers a known rejection through the in-process Arc–Chronicle integration. `NotCommitted` means Arc knows the event transaction did not commit; `Completed` means the required compensation callbacks returned. Neither value proves that a production provider durably canceled a reservation.

Keep the real constraint in this test: a fabricated failed `CommandResult` would skip the commit behavior you are checking. Use provider-backed or transport integration tests for durable cancellation and uncertain commit acknowledgments, rather than adding a network dependency to every decision spec.

For the other side of the boundary, execute with a previously unused organization number and assert successful event persistence, completed operation execution, `Committed` disposition, and no provider cancellation. A later presentation failure is not permission to reverse an already-committed business action.

Continue with [Chronicle transactional commands](../chronicle/commands/transactional-commands.md) for transaction boundaries and [operation recovery](../commands/operations/reference.md#commit-and-recovery) for conservative handling of committed or unknown outcomes.
