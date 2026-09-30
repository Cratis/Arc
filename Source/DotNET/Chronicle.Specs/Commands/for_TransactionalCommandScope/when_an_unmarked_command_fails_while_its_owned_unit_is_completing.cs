// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

/// <summary>
/// A failed unmarked command must roll back its owned unit through Chronicle's public Rollback, exactly as commands did before
/// decision reads existed. That path tolerates a commit that is still in flight, whereas rolling back with the owner capability
/// is refused while the unit is completing; the capability is only for units with enrolled decision reads.
/// </summary>
public class when_an_unmarked_command_fails_while_its_owned_unit_is_completing : Specification
{
    CorrelationId _correlationId;
    TransactionalCommandScope _scope;
    CommandContext _context;
    CommandResult _result;
    UnitOfWorkManager _unitOfWorkManager;
    UnitOfWork _unitOfWork;
    Task _commit;
    TaskCompletionSource<AppendManyResult> _append;
    Exception? _error;
    bool _commitStillInFlight;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventSequence = Substitute.For<IEventSequence>();
        eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(_ => _append.Task);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(eventSequence);
        _unitOfWorkManager = new UnitOfWorkManager(eventStore);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(_unitOfWorkManager);
        _scope = new();
        _context = new(_correlationId, typeof(object), new object(), [], new(), ServiceProvider: serviceProvider);
    }

    async Task Because()
    {
        _scope.Begin(_context);
        _unitOfWork = (UnitOfWork)_unitOfWorkManager.Current;
        _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.New(), new object(), Causation.Unknown());

        // A commit that has begun but not finished, such as an aggregate root committing the command's unit.
        _commit = _unitOfWork.Commit();
        _result = CommandResult.Error(_correlationId, "operation failed");
        _error = await Catch.Exception(() => _scope.Complete(_context, _result));
        _commitStillInFlight = !_commit.IsCompleted;

        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _commit;
    }

    [Fact] void should_not_fail_completing_the_command() => _error.ShouldBeNull();
    [Fact] void should_have_rolled_the_unit_back_while_the_commit_was_in_flight() => (_commitStillInFlight && _unitOfWork.IsCompleted).ShouldBeTrue();
    [Fact] void should_report_known_noncommit() => _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.NotCommitted);
    [Fact] void should_keep_the_command_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_add_an_owner_refusal() => _result.ExceptionMessages.ShouldContainOnly("operation failed");
}
