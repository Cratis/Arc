// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_an_async_protected_read_yields_to_a_single_thread_command_pump : given.asynchronous_command_binding
{
    readonly TaskCompletionSource<DecisionRead<Balance>> _acquisition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Balance> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    DecisionRead<Balance> _token;
    Command _command;
    CommandResult _result;
    int _pumpThread;
    int _handlerThread;

    void Establish()
    {
        _token = DecisionFixtures.Protected("from", _from);
        _command = new(() => _unit.HasEnrolledDecisionReads, () => _handlerThread = Environment.CurrentManagedThreadId);
        _inner.GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            SynchronizationContext.Current!.Post(_ => _acquisition.TrySetResult(_token), null);
            return Acquire();
        });
        _stored.Release(_from).Returns(_ =>
        {
            SynchronizationContext.Current!.Post(_ => _release.TrySetResult(_from), null);
            return _release.Task;
        });
    }

    async Task<DecisionRead<Balance>> Acquire()
    {
        var token = await _acquisition.Task;
        _unit.AddDecisionRead(token);
        return token;
    }

    Task Because() => given.SingleThreadCommandPump.Run(async () =>
    {
        _pumpThread = Environment.CurrentManagedThreadId;
        _result = await Execute(_command);
    }, () =>
    {
        _acquisition.TrySetResult(_token);
        _release.TrySetResult(_from);
    });

    [Fact] void should_finish_the_pipeline() => Assert.True(_result.IsSuccess, string.Join(Environment.NewLine, _result.ExceptionMessages));
    [Fact] void should_invoke_handle_on_the_pump() => _handlerThread.ShouldEqual(_pumpThread);
    [Fact] void should_enroll_before_handle() => _command.Enrolled.ShouldBeTrue();
    [Fact] void should_share_the_provided_instance_with_handle() => _command.Handled.ShouldBeSame(_command.Provided);
    [Fact] void should_bind_the_cached_decision_wrapper() => _command.Token.ShouldBeSame(_token);
    [Fact] void should_acquire_only_once() => _inner.Received(1).GetFor<Balance>(_unit, (ReadModelKey)"from", Arg.Any<CancellationToken>());
    [Fact] void should_release_only_once() => _stored.Received(1).Release(_from);

    [ReadProtection(ReadProtectionMode.On)]
    public class Command(Func<bool> enrolled, Action handled)
    {
        public AccountId From { get; } = new("from");
        public Balance? Provided { get; private set; }
        public Balance? Handled { get; private set; }
        public DecisionRead<Balance>? Token { get; private set; }
        public bool Enrolled { get; private set; }
        public Balance Provide([LookUpBy(nameof(From))] Balance balance) => Provided = balance;
        public void Handle(Balance balance, DecisionRead<Balance> token)
        {
            Handled = balance;
            Token = token;
            Enrolled = enrolled();
            handled();
        }
    }
}
