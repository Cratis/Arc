// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

/// <summary>
/// An unannotated controller uses ambient decision reads while a nested pipeline command owns its policy.
/// </summary>
public class and_the_controller_reads_a_read_model : given.a_controller_invoking_the_pipeline
{
    readonly List<DecisionRead<Model>> _reads = [];
    readonly List<bool> _controllerEnforcement = [];
    IDecisionReads _inner;
    DecisionRead<Model> _read;
    bool _nestedPolicyActive;
    ReadProtectionMode _nestedProtection;
    bool _nestedEnforcement;

    /// <summary>
    /// Configures the real command-aware reader and observations inside the nested handler.
    /// </summary>
    void Establish()
    {
        _inner = Substitute.For<IDecisionReads>();
        _read = DecisionRead<Model>.Unprotected((ReadModelKey)"source", new Model());
        _inner.Get<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>()).Returns(_read);
        var reader = new CommandDecisionReads(_inner, _store, Substitute.For<IReadModels>());
        _handle = _ =>
        {
            _nestedPolicyActive = CommandDecisionPolicy.IsActive;
            _nestedProtection = CommandDecisionPolicy.ReadProtection;
            _nestedEnforcement = _manager.Current.IsEnforcingReadProtection;
            return Task.FromResult<object?>(null);
        };
        _action = async () =>
        {
            _controllerEnforcement.Add(_manager.Current.IsEnforcingReadProtection);
            _reads.Add(await reader.Get<Model>((ReadModelKey)"source"));
            _innerResult = await Execute(new EnforcingCommand());
            await Task.Yield();
            _reads.Add(await reader.Get<Model>((ReadModelKey)"source"));
            _controllerEnforcement.Add(_manager.Current.IsEnforcingReadProtection);
        };
    }

    /// <summary>
    /// Executes the controller and its nested pipeline command.
    /// </summary>
    /// <returns>The asynchronous controller execution.</returns>
    Task Because() => Invoke();

    /// <summary>
    /// Neither controller read is rejected by decision-policy verification.
    /// </summary>
    [Fact] void should_return_both_ambient_reads() => _reads.ShouldEqual([_read, _read]);

    /// <summary>
    /// Controller reads reach Chronicle's ambient read path.
    /// </summary>
    /// <returns>The asynchronous call verification.</returns>
    [Fact] Task should_use_the_real_ambient_read_path() => _inner.Received(2).Get<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>());

    /// <summary>
    /// The controller does not install an enforcing unit.
    /// </summary>
    [Fact] void should_not_enforce_controller_reads() => _controllerEnforcement.ShouldEqual([false, false]);

    /// <summary>
    /// The nested pipeline command establishes its own policy.
    /// </summary>
    [Fact] void should_activate_the_nested_policy() => _nestedPolicyActive.ShouldBeTrue();

    /// <summary>
    /// The nested pipeline command honors its declared read protection.
    /// </summary>
    [Fact] void should_protect_the_nested_command() => _nestedProtection.ShouldEqual(ReadProtectionMode.On);

    /// <summary>
    /// The nested pipeline command receives an enforcing unit.
    /// </summary>
    [Fact] void should_enforce_nested_reads() => _nestedEnforcement.ShouldBeTrue();

    /// <summary>
    /// The controller succeeds without decision-read verification exceptions.
    /// </summary>
    [Fact] void should_return_controller_success() => ControllerResult.IsSuccess.ShouldBeTrue();

    /// <summary>
    /// The nested command completes successfully in its own transaction.
    /// </summary>
    [Fact] void should_return_nested_success() => _innerResult.IsSuccess.ShouldBeTrue();

    /// <summary>
    /// The model returned by the ambient reader.
    /// </summary>
    sealed record Model;
}
