// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandUnionMetadata;

public class when_registering_a_declaration : Specification
{
    Type[] _cases;
    CommandUnionMetadata.Declaration _declaration;

    void Establish() => _cases = [typeof(string), typeof(int)];

    void Because()
    {
        CommandUnionMetadata.Register(typeof(Declaration), _cases, value => ((Declaration)value).Value);
        CommandUnionMetadata.Register(typeof(Declaration), _cases, value => ((Declaration)value).Value);
        _cases[0] = typeof(decimal);
        _declaration = CommandUnionMetadata.For(typeof(Declaration))!;
    }

    [Fact] void should_snapshot_the_case_types() => _declaration.CaseTypes.ShouldEqual<Type>([typeof(string), typeof(int)]);
    [Fact] void should_expose_a_read_only_case_list() => ((ICollection<Type>)_declaration.CaseTypes).IsReadOnly.ShouldBeTrue();
    [Fact] void should_use_the_registered_accessor() => _declaration.ActiveValue(new Declaration("value")).ShouldEqual("value");
    [Fact] void should_not_resolve_an_unregistered_type() => CommandUnionMetadata.For(typeof(Unregistered)).ShouldBeNull();

    record Declaration(string Value);
    record Unregistered;
}
