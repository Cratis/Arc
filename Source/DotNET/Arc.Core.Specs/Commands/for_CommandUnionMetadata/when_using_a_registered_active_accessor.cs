// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
namespace Cratis.Arc.Commands.for_CommandUnionMetadata;

public class when_using_a_registered_active_accessor : Specification
{
    object _active;

    void Establish() => CommandUnionMetadata.Register(typeof(Outcome), [typeof(string), typeof(int)], _ => "registered");
    void Because() => _active = CommandUnionValue.Active(new Outcome(42));

    [Fact] void should_dispatch_through_the_registered_delegate() => _active.ShouldEqual("registered");

#pragma warning disable CA1815 // Compiler-generated union; equality is not used for command returns.
    public readonly union Outcome(string, int);
#pragma warning restore CA1815
}
#endif
