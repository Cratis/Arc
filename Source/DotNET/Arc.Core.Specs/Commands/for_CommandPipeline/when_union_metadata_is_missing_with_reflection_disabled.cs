// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_union_metadata_is_missing_with_reflection_disabled : given.a_native_union_pipeline
{
    Command _command;

    async Task Because()
    {
        if (CommandUnionFeatureSwitches.ReflectionIsSupported)
        {
            await for_CommandUnionMetadata.given.union_reflection_disabled_process.Run(GetType());
            return;
        }
        _command = new Command();
        await Run(_command);
    }

    [Fact]
    void should_fail_clearly_before_handle_without_weakening_the_boundary()
    {
        if (CommandUnionFeatureSwitches.ReflectionIsSupported) return; // The isolated process runs the assertions.
        _result.IsSuccess.ShouldBeFalse();
        _result.ExceptionMessages.ShouldContainOnly(new MissingCommandUnionMetadata(typeof(Missing)).Message);
        _result.ExceptionStackTrace.ShouldContain(nameof(CommandUnionMetadata));
        _command.Calls.ShouldEqual(0);
    }

#pragma warning disable CA1815 // Compiler-generated union; equality is not used for command returns.
    public readonly union Missing(string, int);
#pragma warning restore CA1815
    /// <summary>
    /// Deliberately compiled without CommandAttribute: no metadata, as with an assembly without the generator.
    /// </summary>
    public class Command
    {
        public int Calls { get; private set; }
        public Missing Handle()
        {
            Calls++;
            return "response";
        }
    }
}
#endif
