// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

/// <summary>
/// A controller body sharing a guarded pipeline handler does not enroll generated-parameter contributors.
/// </summary>
public class when_the_body_shares_a_guarded_pipeline_handler : given.a_controller_command_with_a_transactional_scope
{
    ICommandHandlerProviders _providers;

    /// <summary>
    /// Supplies the same command type and guarded handler that the pipeline can resolve.
    /// </summary>
    void Establish()
    {
        var command = new Commands.given.a_generated_target.CreatesId();
        _executing.ActionArguments["command"] = command;
        _executing.ActionDescriptor.Parameters[0].ParameterType = command.GetType();
        _unitOfWork = Substitute.For<UnitOfWorkWithNamedTags>();
        _unitOfWorkManager.Begin(Arg.Any<CorrelationId>()).Returns(_unitOfWork);
        var handler = Substitute.For<ICommandHandler>();
        handler.Parameters.Returns(command.GetType().GetMethod("Handle")!.GetParameters());
        _providers = Substitute.For<ICommandHandlerProviders>();
        _providers.TryGetHandlerFor(command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
        _serviceProvider.GetService(typeof(ICommandHandlerProviders)).Returns(_providers);
    }

    /// <summary>
    /// Executes the controller using the shared body type.
    /// </summary>
    /// <returns>The asynchronous controller execution.</returns>
    Task Because() => _filter.OnActionExecutionAsync(_executing, () => Task.FromResult(_executed));

    /// <summary>
    /// Controller execution never discovers the pipeline handler's generated parameters.
    /// </summary>
    [Fact] void should_not_look_up_a_pipeline_handler() => _providers.DidNotReceive().TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>());

    /// <summary>
    /// No generated-parameter enrollment contributor is registered on the controller unit.
    /// </summary>
    [Fact] void should_not_register_an_enrollment_contributor() => _unitOfWork.DidNotReceive().AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());

    /// <summary>
    /// The controller retains its normal successful outcome.
    /// </summary>
    [Fact] void should_succeed() => Result.IsSuccess.ShouldBeTrue();
}
