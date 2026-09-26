// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_with_validation_policy : a_command_pipeline
{
    [Command]
    [BlockOnValidationSeverity(ValidationResultSeverity.Information)]
    protected record StrictCommand;

    [Command]
    protected record DerivedStrictCommand : StrictCommand;

    [Command]
    [BlockOnValidationSeverity((ValidationResultSeverity)99)]
    protected record InvalidPolicyCommand;

    [Command]
    protected record LegacyCommand;

    protected ICommandHandler _handler;
    protected ValidationResultSeverity _failureSeverity;
    protected object _command;

    void Establish()
    {
        _command = new StrictCommand();
        _handler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(_command, out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ => Task.FromResult(new CommandResult
        {
            CorrelationId = _correlationId,
            ValidationResults = [new ValidationResult(_failureSeverity, "Keep this message", [], null!)]
        }));
    }
}
