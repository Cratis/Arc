// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

/// <summary>
/// An aggregate keeps the existing ability to reuse and commit the controller's ambient unit.
/// </summary>
public class and_an_aggregate_commits_the_controller_unit : given.a_controller_invoking_the_pipeline
{
    IUnitOfWork _aggregateUnit;
    int _appendCount;
    bool _completedInsideAction;
    AggregateRootCommitResult _aggregateResult;

    /// <summary>
    /// Configures the real aggregate factory, mutation and commit on the controller's unit.
    /// </summary>
    void Establish()
    {
        var append = _append;
        _append = events =>
        {
            _appendCount++;
            return append(events);
        };
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.HasFor(typeof(string)).Returns(true);
        _store.EventTypes.Returns(eventTypes);
        var mutators = Substitute.For<IAggregateRootMutatorFactory>();
        mutators.Create<AggregateRoot>(Arg.Any<IAggregateRootContext>()).Returns(call =>
        {
            _aggregateUnit = call.Arg<IAggregateRootContext>().UnitOfWOrk;
            return Substitute.For<IAggregateRootMutator>();
        });
        var factory = new AggregateRootFactory(_store, mutators, _manager, _services);
        _action = async () =>
        {
            Stage("controller");
            var aggregate = await factory.Get<AggregateRoot>(EventSourceId.New());
            await aggregate.Apply("aggregate");
            _aggregateResult = await aggregate.Commit();
            _completedInsideAction = _aggregateUnit.IsCompleted;
        };
    }

    /// <summary>
    /// Executes the controller and its aggregate commit.
    /// </summary>
    /// <returns>The asynchronous controller execution.</returns>
    Task Because() => Invoke();

    /// <summary>
    /// The aggregate reuses the unit established for the controller.
    /// </summary>
    [Fact] void should_reuse_the_controller_unit() => _aggregateUnit.ShouldBeSame(_controllerUnit);

    /// <summary>
    /// The aggregate still commits before the controller returns.
    /// </summary>
    [Fact] void should_complete_inside_the_action() => _completedInsideAction.ShouldBeTrue();

    /// <summary>
    /// The controller scope does not append an already committed unit again.
    /// </summary>
    [Fact] void should_append_once() => _appendCount.ShouldEqual(1);

    /// <summary>
    /// The ambient commit includes the controller and aggregate events.
    /// </summary>
    [Fact] void should_commit_both_events() => _committed.ShouldContainOnly("controller", "aggregate");

    /// <summary>
    /// The aggregate commit retains its successful outcome.
    /// </summary>
    [Fact] void should_return_aggregate_success() => _aggregateResult.IsSuccess.ShouldBeTrue();

    /// <summary>
    /// The controller retains its successful outcome.
    /// </summary>
    [Fact] void should_return_controller_success() => ControllerResult.IsSuccess.ShouldBeTrue();
}
