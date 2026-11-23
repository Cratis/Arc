// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_an_operation_precedes_missing_union_metadata : given.a_native_union_pipeline
{
    CountingCommand[] _commands;
    readonly List<CommandResult> _results = [];

    async Task Because()
    {
        if (CommandUnionFeatureSwitches.ReflectionIsSupported)
        {
            await for_CommandUnionMetadata.given.union_reflection_disabled_process.Run(GetType());
            return;
        }
        CommandUnionMetadata.Register(typeof(Outcome), [typeof(CommandOperations), typeof(Missing)], value => ((IUnion)value).Value);
        _commands = [new Tuple(), new Collection(), new Wrapped(), new Native(), new Awaited(), new Overloaded()];
        foreach (var command in _commands)
        {
            await Run(command);
            _results.Add(_result);
        }
    }

    [Fact]
    void should_validate_every_declaration_before_any_handle_side_effect()
    {
        if (CommandUnionFeatureSwitches.ReflectionIsSupported) return; // The isolated process runs the assertions.
        _results.Count.ShouldEqual(6);
        foreach (var result in _results)
        {
            result.IsSuccess.ShouldBeFalse();
            result.ExceptionMessages.ShouldContainOnly(new MissingCommandUnionMetadata(typeof(Missing)).Message);
        }
        foreach (var command in _commands) command.Calls.ShouldEqual(0);
        CommandOperationBoundary.IsActive.ShouldBeFalse();
    }

#pragma warning disable CA1815 // Compiler-generated unions; equality is not used for command returns.
    public readonly union Missing(string, int);
    public readonly union Outcome(CommandOperations, Missing);
#pragma warning restore CA1815
    public class CountingCommand
    {
        public int Calls { get; private set; }
        protected T Called<T>()
        {
            Calls++;
            throw new InvalidOperationException("Handle must not run before every union declaration is validated.");
        }
    }
    public class Tuple : CountingCommand { public (CommandOperations, Missing) Handle() => Called<(CommandOperations, Missing)>(); }
    public class Collection : CountingCommand { public List<(CommandOperations, Missing)> Handle() => Called<List<(CommandOperations, Missing)>>(); }
    public class Wrapped : CountingCommand { public OneOf<CommandOperations, Missing> Handle() => Called<OneOf<CommandOperations, Missing>>(); }
    public class Native : CountingCommand { public Outcome Handle() => Called<Outcome>(); }
    public class Awaited : CountingCommand { public Task<(CommandOperations, Missing)> Handle() => Task.FromResult(Called<(CommandOperations, Missing)>()); }
    public class Overloaded : CountingCommand
    {
        public CommandOperations Handle() => Called<CommandOperations>();
        public Missing Handle(int input) => Called<Missing>();
    }
}
#endif
