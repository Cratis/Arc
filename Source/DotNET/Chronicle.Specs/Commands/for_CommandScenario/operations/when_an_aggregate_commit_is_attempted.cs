// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public class when_an_aggregate_commit_is_attempted : Specification
{
    CommandScenario<CommitThenReserve> _scenario;
    IUnitOfWork _unitOfWork;
    CommandResult _result;

    void Establish()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        var context = Substitute.For<IAggregateRootContext>();
        context.UnitOfWOrk.Returns(_unitOfWork);
        var mutation = new AggregateRootMutation(context, Substitute.For<IAggregateRootMutator>(), Substitute.For<IEventSequence>());
        _scenario = new();
        _scenario.Services.AddSingleton<IAggregateRootMutation>(mutation);
    }

    async Task Because() => _result = await _scenario.Execute(new CommitThenReserve());
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_reject_the_explicit_aggregate_commit() => _result.ShouldNotBeSuccessful();
    [Fact] void should_guard_before_the_aggregate_calls_commit() => _unitOfWork.DidNotReceive().Commit();
    [Fact] void should_start_no_operations() => _scenario.ShouldHaveNoOperationInvocations();

    [Command]
    public record CommitThenReserve
    {
        public async Task<CommandOperations> Handle(IAggregateRootMutation mutation)
        {
            await mutation.Commit();
            return [];
        }
    }
}
