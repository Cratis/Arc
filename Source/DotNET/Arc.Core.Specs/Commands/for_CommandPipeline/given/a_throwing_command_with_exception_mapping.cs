// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_throwing_command_with_exception_mapping : a_command_pipeline_and_a_handler_for_command
{
    protected Exception _exception;
    protected ICommandExceptionMapper _mapper;
    protected CommandResult _mapped;
    protected CommandResult _result;

    void Establish()
    {
        _exception = new Exception("original failure");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Throws(_exception);
        _mapper = Substitute.For<ICommandExceptionMapper>();
        _mapped = new() { CorrelationId = _correlationId, ValidationResults = [ValidationResult.Error("mapped rejection")] };
        _serviceProvider.GetService(typeof(IInstancesOf<ICommandExceptionMapper>)).Returns(new KnownInstancesOf<ICommandExceptionMapper>([_mapper]));
    }
}
