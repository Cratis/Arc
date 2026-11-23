// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_a_harmless_union_collection_is_returned : given.a_native_union_pipeline
{
    Command _command;

    void Establish() => _command = new Command();
    async Task Because() => await Run(_command);

    [Fact] void should_generate_metadata_for_collection_elements() => CommandUnionMetadata.For(typeof(Element)).ShouldNotBeNull();
    [Fact] void should_not_activate_an_operation_boundary() => _command.BoundaryWasActive.ShouldBeFalse();
    [Fact] void should_preserve_the_collection_as_a_response() => _result.ResponseValue.ShouldBeOfExactType<List<Element>>();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

#pragma warning disable CA1815 // Compiler-generated union; equality is not used for command returns.
    public readonly union Element(int, string);
#pragma warning restore CA1815
    [Command]
    public class Command
    {
        public bool BoundaryWasActive { get; private set; }
        public List<Element> Handle()
        {
            BoundaryWasActive = CommandOperationBoundary.IsActive;
            return [new Element(42)];
        }
    }
}
#endif
