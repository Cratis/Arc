// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandUnionMetadata;

public class when_registering_after_a_negative_lookup : Specification
{
    CommandUnionMetadataRegistry _registry;
    CommandUnionMetadata.Declaration _declaration;
    int _registrations;

    void Establish() => _registry = new(() => _registrations++);

    void Because()
    {
        _registry.For(typeof(Outcome)).ShouldBeNull();
        _registry.Register(typeof(Outcome), [typeof(string)], value => ((Outcome)value).Value);
        _declaration = _registry.For(typeof(Outcome));
    }

    [Fact] void should_resolve_the_late_declaration() => _declaration.CaseTypes.ShouldEqual<Type>([typeof(string)]);
    [Fact] void should_dispatch_through_the_late_accessor() => _declaration.ActiveValue(new Outcome("registered")).ShouldEqual("registered");
    [Fact] void should_not_reactivate_modules() => _registrations.ShouldEqual(1);

    record Outcome(string Value);
}
