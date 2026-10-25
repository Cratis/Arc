// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

/// <summary>
/// Read-protection declarations on MVC body types do not opt controllers into pipeline semantics.
/// </summary>
public class when_the_body_declares_read_protection : given.a_controller_command_with_a_transactional_scope
{
    readonly List<ReadProtectionMode> _protection = [];
    readonly List<bool> _measuring = [];
    readonly List<bool> _success = [];
    IEventLog _log;

    /// <summary>
    /// Registers the actual measurement scope so an accidental policy would trigger its tail read.
    /// </summary>
    void Establish()
    {
        _unitOfWork = Substitute.For<UnitOfWorkWithNamedTags>();
        _unitOfWorkManager.Begin(Arg.Any<CorrelationId>()).Returns(_unitOfWork);
        _log = Substitute.For<IEventLog>();
        _serviceProvider.GetService(typeof(IEventLog)).Returns(_log);
        var scopes = new KnownInstancesOf<ICommandExecutionScope>([_scope, new ReadProtectionMeasureScope()]);
        _serviceProvider.GetService(typeof(IInstancesOf<ICommandExecutionScope>)).Returns(scopes);
    }

    /// <summary>
    /// Executes bodies declaring enforcement and measurement respectively.
    /// </summary>
    /// <returns>The asynchronous controller executions.</returns>
    async Task Because()
    {
        foreach (var body in new object[] { new EnforcingBody(), new MeasuringBody() })
        {
            _executing.ActionArguments["command"] = body;
            _executing.ActionDescriptor.Parameters[0].ParameterType = body.GetType();
            await _filter.OnActionExecutionAsync(_executing, () =>
            {
                _protection.Add(CommandDecisionPolicy.ReadProtection);
                _measuring.Add(ReadProtectionMeasurement.Current is not null);
                return Task.FromResult(_executed);
            });
            _success.Add(Result.IsSuccess);
        }
    }

    /// <summary>
    /// Neither body declaration changes the effective read protection.
    /// </summary>
    [Fact] void should_leave_read_protection_off() => _protection.ShouldEqual([ReadProtectionMode.Off, ReadProtectionMode.Off]);

    /// <summary>
    /// Neither body declaration opens a measurement context.
    /// </summary>
    [Fact] void should_not_measure_controller_reads() => _measuring.ShouldEqual([false, false]);

    /// <summary>
    /// Neither body declaration installs enforcement on the controller unit.
    /// </summary>
    [Fact] void should_not_enforce_the_controller_unit() => _unitOfWork.DidNotReceive().EnforceReadProtection();

    /// <summary>
    /// Measurement never reads the event-log tail for a controller body declaration.
    /// </summary>
    /// <returns>The asynchronous call verification.</returns>
    [Fact] Task should_not_read_the_event_log_tail() => _log.DidNotReceiveWithAnyArgs().GetTailSequenceNumber();

    /// <summary>
    /// Both controller executions retain their successful responses.
    /// </summary>
    [Fact] void should_succeed_for_both_declarations() => _success.ShouldEqual([true, true]);

    /// <summary>
    /// An MVC body that declares enforcing read protection for pipeline use.
    /// </summary>
    [ReadProtection(ReadProtectionMode.On)]
    sealed record EnforcingBody;

    /// <summary>
    /// An MVC body that declares observational read protection for pipeline use.
    /// </summary>
    [ReadProtection(ReadProtectionMode.Measure)]
    sealed record MeasuringBody;
}
