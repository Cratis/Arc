// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_resolving_an_asynchronous_decision_read_parameter : for_CommandDecisionReads.given.asynchronous_command_binding
{
    readonly TaskCompletionSource<DecisionRead<Balance>> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    DecisionRead<Balance> _token;
    Command _command;
    CommandResult _result;

    void Establish()
    {
        _token = for_CommandDecisionReads.DecisionFixtures.Protected("from", _from);
        _command = new();
        _inner.GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            SynchronizationContext.Current!.Post(_ => _completion.TrySetResult(_token), null);
            return Acquire();
        });
    }

    async Task<DecisionRead<Balance>> Acquire()
    {
        var token = await _completion.Task;
        _unit.AddDecisionRead(token);
        return token;
    }

    Task Because() => for_CommandDecisionReads.given.SingleThreadCommandPump.Run(async () => _result = await Execute(_command), () => _completion.TrySetResult(_token));

    [Fact] void should_finish_without_a_synchronous_wrapper_registration() => Assert.True(_result.IsSuccess, string.Join(Environment.NewLine, _result.ExceptionMessages));
    [Fact] void should_bind_the_issued_wrapper() => _command.Read.ShouldBeSame(_token);
    [Fact] void should_enroll_the_wrapper() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();

    [ReadProtection(ReadProtectionMode.On)]
    public class Command
    {
        public DecisionRead<Balance>? Read { get; private set; }
        public void Handle(DecisionRead<Balance> read) => Read = read;
    }
}
