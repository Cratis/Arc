// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_collection_declares_an_inactive_union_operation : given.an_operation_pipeline
{
    Command _command;

    void Establish() => _command = new Command();
    async Task Because() => _result = await Run(_command);

    [Fact] void should_use_generated_declarations() => CommandUnionMetadata.For(typeof(Outcome)).ShouldNotBeNull();
    [Fact] void should_activate_the_boundary_for_an_inactive_operation() => _command.BoundaryWasActive.ShouldBeTrue();
    [Fact] void should_reject_the_collection_instead_of_executing_it() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_execute_an_operation() => _log.Calls.ShouldBeEmpty();

#pragma warning disable CA1815 // Compiler-generated union; equality is not used for command returns.
    public readonly union Outcome(given.NamedOperation, string);
#pragma warning restore CA1815
    [Command]
    public class Command
    {
        public bool BoundaryWasActive { get; private set; }
        public IEnumerable<Outcome> Handle()
        {
            BoundaryWasActive = CommandOperationBoundary.IsActive;
            return [new Outcome("response")];
        }
    }
}
#endif
