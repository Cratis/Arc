// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_native_union_pipeline : a_command_pipeline
{
    protected CommandResult _result;
    protected Receipt _receipt = new("42");
    protected Recorded _recorded = new("created");

    void Establish()
    {
        _commandHandlerProviders.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            var type = call[0].GetType();
            call[1] = new ModelBoundCommandHandler(type, type.GetMethod("Handle")!);
            return true;
        });
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>())
            .Returns(call => call[1] is Recorded or ValidationResult);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), Arg.Any<object>())
            .Returns(call => call[1] is ValidationResult validation
                ? new CommandResult { ValidationResults = [validation] }
                : CommandResult.Success(_correlationId));
    }

    protected async Task Run(object command) => _result = await _commandPipeline.Execute(command, _serviceProvider);

    public record Receipt(string Number);
    public record Recorded(string Name);

    // Compiler-generated union structs; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Answer(Receipt, int, Recorded, ValidationResult);
    public readonly union Nested(OneOf<Answer, string>, (Receipt, Recorded));
#pragma warning restore CA1815
    public record Direct(Answer Value) { public Answer Handle() => Value; }
    public record Awaited(Answer Value) { public Task<Answer> Handle() => Task.FromResult(Value); }
    public record ValueAwaited(Answer Value) { public ValueTask<Answer> Handle() => new(Value); }
    public record Wrapped(Nested Value) { public Nested Handle() => Value; }
    public record Tuple(Recorded Event, Answer Value) { public (Recorded, Answer) Handle() => (Event, Value); }
}
#endif
